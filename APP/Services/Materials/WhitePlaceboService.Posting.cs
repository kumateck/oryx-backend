using APP.Utils;
using DOMAIN.Entities.BinCards;
using DOMAIN.Entities.Grns;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Materials.WhitePlacebo;
using DOMAIN.Entities.Requisitions;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Services.Materials;

public sealed partial class WhitePlaceboService
{
    public async Task<Result> Issue(Guid id)
    {
        var scope = await Department(PermissionKeys.CanIssueWhitePlaceboIngredients);
        if (scope.IsFailure) return scope.Errors;
        await using var transaction = context.Database.IsRelational()
            ? await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable) : null;
        var item = await Preparations(scope.Value).Include(preparation => preparation.Requisition.Items).SingleOrDefaultAsync(item => item.Id == id);
        if (item == null) return Error.NotFound("WhitePlacebo.NotFound", "Preparation was not found in your department.");
        if (item.IssuedAt.HasValue) return Result.Success();
        if (!item.Requisition.Approved || item.Requisition.Status is RequestStatus.Rejected or RequestStatus.Completed)
            return Error.Conflict("WhitePlacebo.Approval", "Ingredients can be issued only against this preparation's approved, unissued stock requisition.");
        var requested = item.Inputs.GroupBy(input => new { input.MaterialId, input.UoMId }).ToList();
        if (requested.Count != item.Requisition.Items.Count || requested.Any(group => !item.Requisition.Items.Any(line =>
            line.MaterialId == group.Key.MaterialId && line.UoMId == group.Key.UoMId && line.Quantity == group.Sum(input => input.Quantity))))
            return Error.Conflict("WhitePlacebo.RequestChanged", "The approved request no longer matches the preparation inputs. No stock was issued.");
        var ids = item.Inputs.Select(input => input.SourceShelfId).ToArray();
        var shelves = await Shelves(scope.Value).Where(shelf => ids.Contains(shelf.Id)).ToListAsync();
        if (shelves.Count != ids.Length)
            return Error.Conflict("WhitePlacebo.StockChanged", "An ingredient is no longer on its selected shelf. No stock was issued.");
        var now = DateTime.UtcNow;
        foreach (var batch in shelves.GroupBy(shelf => shelf.MaterialBatchId))
            if (item.Inputs.Where(input => input.MaterialBatchId == batch.Key).Sum(input => input.Quantity) > batch.First().MaterialBatch.RemainingQuantity)
                return Error.Conflict("WhitePlacebo.Reserved", "An ingredient is reserved for other production. No stock was issued.");
        foreach (var input in item.Inputs)
        {
            var shelf = shelves.Single(shelf => shelf.Id == input.SourceShelfId);
            if (shelf.MaterialBatchId != input.MaterialBatchId || shelf.MaterialBatch.UoMId != input.UoMId)
                return Error.Conflict("WhitePlacebo.StockChanged", "An ingredient's batch or unit changed. No stock was issued.");
            var valid = ValidateStock(shelf, input.Quantity, now);
            if (valid.IsFailure) return valid;
        }
        // Validate all inputs before staging any stock movement. The surrounding transaction and
        // quantity concurrency tokens protect the complete posting, including issue signatures.
        foreach (var group in item.Inputs.GroupBy(input => new { input.MaterialId, input.SourceWarehouseId }))
        {
            var history = await context.BinCardInformation.Where(card => card.MaterialBatch.MaterialId == group.Key.MaterialId
                && card.WarehouseId == group.Key.SourceWarehouseId)
                .Select(card => new { card.QuantityReceived, card.QuantityIssued }).ToListAsync();
            var balance = history.Sum(card => card.QuantityReceived - card.QuantityIssued);
            foreach (var input in group)
            {
                var shelf = shelves.Single(shelf => shelf.Id == input.SourceShelfId);
                shelf.Quantity -= input.Quantity;
                shelf.MaterialBatch.ConsumedQuantity += input.Quantity;
                shelf.MaterialBatch.QuantityAssigned = Math.Max(0, shelf.MaterialBatch.QuantityAssigned - input.Quantity);
                balance -= input.Quantity;
                context.MaterialBatchEvents.Add(new MaterialBatchEvent { Id = Guid.NewGuid(), BatchId = input.MaterialBatchId,
                    Quantity = input.Quantity, Type = EventType.Consumed, UserId = currentUser.UserId.Value,
                    ConsumptionWarehouseId = input.SourceWarehouseId, ConsumedAt = now });
                context.BinCardInformation.Add(new BinCardInformation { Id = Guid.NewGuid(), MaterialBatchId = input.MaterialBatchId,
                    WarehouseId = input.SourceWarehouseId, QuantityIssued = input.Quantity, BalanceQuantity = balance,
                    UoMId = input.UoMId, RequisitionId = item.RequisitionId, RequisitionCode = item.Requisition.Code,
                    ProductBatchNumber = item.BatchNumber, Description = $"White Placebo preparation {item.BatchNumber}" });
            }
        }
        item.IssuedAt = now;
        item.IssuedById = currentUser.UserId;
        item.Requisition.Status = RequestStatus.Completed;
        foreach (var line in item.Requisition.Items) line.Status = RequestStatus.Completed;
        var issue = new StockRequisitionIssue { Id = Guid.NewGuid(), RequisitionId = item.RequisitionId,
            IssuedAt = now, IssuedById = currentUser.UserId.Value, CreatedById = currentUser.UserId };
        context.StockRequisitionIssues.Add(issue);
        var recheck = await Department(PermissionKeys.CanIssueWhitePlaceboIngredients);
        if (recheck.IsFailure || recheck.Value != scope.Value)
            return Error.Forbidden("WhitePlacebo.Permission", "Your permissions changed before issue. No stock was issued.");
        await context.SaveChangesAsync();
        await approvals.CreateInitialApprovalsAsync("StockRequisitionIssue", issue.Id, currentUser.UserId);
        if (transaction != null) await transaction.CommitAsync();
        return Result.Success();
    }

    public async Task<Result> RecordOutput(Guid id, RecordWhitePlaceboOutput body)
    {
        var scope = await Department(PermissionKeys.CanRecordWhitePlaceboOutput);
        if (scope.IsFailure) return scope.Errors;
        if (body == null || body.Quantity <= 0 || body.Quantity > 1_000_000_000_000m
            || body.PreparedAt.Kind != DateTimeKind.Utc || body.PreparedAt > DateTime.UtcNow || string.IsNullOrWhiteSpace(body.Notes))
            return Error.Validation("WhitePlacebo.Output", "Enter the positive net weight, actual preparation time and weighing/variance notes.");
        await using var transaction = context.Database.IsRelational()
            ? await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable) : null;
        var item = await Preparations(scope.Value).SingleOrDefaultAsync(item => item.Id == id);
        if (item == null) return Error.NotFound("WhitePlacebo.NotFound", "Preparation was not found in your department.");
        if (item.OutputBatchId.HasValue)
            return item.ActualQuantity == body.Quantity && item.PreparedAt == body.PreparedAt && item.OutputNotes == body.Notes.Trim()
                ? Result.Success() : Error.Conflict("WhitePlacebo.AlreadyRecorded", "Output is already recorded. Its stock cannot be posted again or overwritten.");
        if (!item.IssuedAt.HasValue || !item.Requisition.Approved || item.Requisition.Status != RequestStatus.Completed)
            return Error.Conflict("WhitePlacebo.NotIssued", "The approved ingredients must be issued before output is recorded.");
        if (body.PreparedAt < item.IssuedAt)
            return Error.Validation("WhitePlacebo.PreparedAt", "Preparation cannot precede the ingredient issue.");
        if (!await context.MaterialDepartments.AnyAsync(link => link.DepartmentId == scope.Value
            && link.MaterialId == item.MaterialId && link.UoMId == item.UoMId && link.Material.IsWhitePlacebo && !link.Material.IsUnlimited))
            return Error.Conflict("WhitePlacebo.UnitChanged", "The output material's department link or unit changed. Restore the recorded unit before posting output.");
        if (await context.MaterialBatches.AnyAsync(batch => batch.MaterialId == item.MaterialId && batch.BatchNumber == item.BatchNumber))
            return Error.Conflict("WhitePlacebo.BatchNumber", "A material batch with this number already exists. Reconcile it before recording output.");
        var now = DateTime.UtcNow;
        // This is an explicitly internal receipt, not a fabricated supplier purchase. It gives QC
        // its existing GRN identity without changing sampling, test, approval or shelving gates.
        var receipt = new Grn { Id = Guid.NewGuid(), GrnNumber = $"WP/{item.Id:N}", CarrierName = "Internal production",
            VehicleNumber = "Not applicable", DeclarationNumber = "Not applicable", Status = Status.Pending,
            Remarks = $"White Placebo {item.BatchNumber}; recipe {item.RecipeReference}; preparation {item.Id}",
            CreatedById = currentUser.UserId };
        var batch = new MaterialBatch { Id = Guid.NewGuid(), MaterialId = item.MaterialId, UoMId = item.UoMId,
            BatchNumber = item.BatchNumber, TotalQuantity = body.Quantity, Status = BatchStatus.Quarantine,
            DateReceived = now, ManufacturingDate = body.PreparedAt, ExpiryDate = body.PreparedAt.AddHours(item.HoldHours),
            Grn = receipt, CreatedById = currentUser.UserId };
        receipt.MaterialBatches = [batch];
        item.OutputBatch = batch;
        item.ActualQuantity = body.Quantity;
        item.PreparedAt = body.PreparedAt;
        item.RecordedAt = now;
        item.RecordedById = currentUser.UserId;
        item.OutputNotes = body.Notes.Trim();
        var recheck = await Department(PermissionKeys.CanRecordWhitePlaceboOutput);
        if (recheck.IsFailure || recheck.Value != scope.Value)
            return Error.Forbidden("WhitePlacebo.Permission", "Your permissions changed before output was recorded.");
        context.Grns.Add(receipt);
        await context.SaveChangesAsync();
        if (transaction != null) await transaction.CommitAsync();
        return Result.Success();
    }
}
