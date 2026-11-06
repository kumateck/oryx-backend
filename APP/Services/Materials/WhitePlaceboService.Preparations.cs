using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.WhitePlacebo;
using DOMAIN.Entities.Requisitions;
using Microsoft.EntityFrameworkCore;
using SHARED;
using APP.Utils;

namespace APP.Services.Materials;

public sealed partial class WhitePlaceboService
{
    private IQueryable<WhitePlaceboPreparation> Preparations(Guid department) => context.WhitePlaceboPreparations
        .Where(item => item.DepartmentId == department).Include(item => item.Material).Include(item => item.UoM)
        .Include(item => item.Requisition).Include(item => item.OutputBatch).Include(item => item.Inputs).AsSplitQuery();

    public async Task<Result<WhitePlaceboPreparationPageDto>> List(int page = 1, int pageSize = 50, string search = "")
    {
        var scope = await Department();
        if (scope.IsFailure) return scope.Errors;
        if (page < 1 || pageSize is < 1 or > 100 || (long)(page - 1) * pageSize > int.MaxValue)
            return Error.Validation("WhitePlacebo.Page", "Choose a valid page with 1 to 100 preparations.");
        var query = Preparations(scope.Value);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(item => item.BatchNumber.Contains(search.Trim()) || item.RecipeReference.Contains(search.Trim()));
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(item => item.CreatedAt).ThenByDescending(item => item.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return new WhitePlaceboPreparationPageDto(items.Select(PreparationDto).ToList(), total);
    }

    public async Task<Result<WhitePlaceboPreparationDto>> Get(Guid id)
    {
        var scope = await Department();
        if (scope.IsFailure) return scope.Errors;
        var item = await Preparations(scope.Value).SingleOrDefaultAsync(item => item.Id == id);
        return item == null ? Error.NotFound("WhitePlacebo.NotFound", "Preparation was not found in your department.") : PreparationDto(item);
    }

    private static WhitePlaceboPreparationDto PreparationDto(WhitePlaceboPreparation item) => new(
        item.Id, item.BatchNumber, item.Material.Name, item.UoM.Symbol, item.RecipeRevision, item.RecipeReference,
        item.Instructions, item.StorageConditions, item.HoldHours, item.ExpectedQuantity, item.ActualQuantity,
        item.Notes, item.OutputNotes, item.RequisitionId, item.Requisition.Approved, item.Requisition.Status == RequestStatus.Rejected,
        item.CreatedAt, item.IssuedById, item.IssuedAt, item.RecordedById, item.RecordedAt, item.PreparedAt,
        item.OutputBatchId, item.OutputBatch?.GrnId, item.OutputBatch?.Status, item.OutputBatch?.ExpiryDate,
        item.Inputs.OrderBy(input => input.MaterialName).ThenBy(input => input.BatchNumber).Select(input => new WhitePlaceboInputDto(
            input.Id, input.MaterialId, input.MaterialBatchId, input.MaterialName, input.MaterialCode,
            input.BatchNumber, input.UnitSymbol, input.RecipeQuantity, input.Quantity)).ToList());

    public async Task<Result<Guid>> Create(CreateWhitePlaceboPreparation body)
    {
        var scope = await Department(PermissionKeys.CanRequestWhitePlaceboIngredients);
        if (scope.IsFailure) return scope.Errors;
        if (body == null || body.Id == Guid.Empty || string.IsNullOrWhiteSpace(body.BatchNumber)
            || body.ExpectedQuantity <= 0 || body.ExpectedQuantity > 1_000_000_000_000m
            || body.Inputs == null || body.Inputs.Count is < 1 or > 200
            || body.Inputs.Any(item => item.Quantity <= 0 || item.Quantity > 1_000_000_000_000m)
            || body.Inputs.Select(item => item.ShelfMaterialBatchId).Distinct().Count() != body.Inputs.Count)
            return Error.Validation("WhitePlacebo.Preparation", "Enter a batch number, expected output and positive ingredient quantities, with each shelf batch selected once.");
        await using var transaction = context.Database.IsRelational()
            ? await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable) : null;
        var existing = await Preparations(scope.Value).SingleOrDefaultAsync(item => item.Id == body.Id);
        if (existing != null)
        {
            var identical = existing.BatchNumber == body.BatchNumber.Trim() && existing.RecipeRevision == body.RecipeRevision
                && existing.ExpectedQuantity == body.ExpectedQuantity && existing.Inputs.Count == body.Inputs.Count
                && (existing.Notes ?? "") == (body.Notes?.Trim() ?? "")
                && existing.Inputs.All(input => body.Inputs.Any(line => line.ShelfMaterialBatchId == input.SourceShelfId && line.Quantity == input.Quantity));
            return identical ? existing.Id : Error.Conflict("WhitePlacebo.RetryChanged", "This request was already saved with different values. Open its preparation before continuing.");
        }
        var recipe = await Recipes(scope.Value).SingleOrDefaultAsync();
        if (recipe == null || recipe.Revision != body.RecipeRevision)
            return Error.Conflict("WhitePlacebo.RecipeChanged", "The recipe is missing or changed. Reload before requesting ingredients.");
        if (await context.WhitePlaceboPreparations.AnyAsync(item => item.MaterialId == recipe.MaterialId && item.BatchNumber == body.BatchNumber.Trim())
            || await context.MaterialBatches.AnyAsync(item => item.MaterialId == recipe.MaterialId && item.BatchNumber == body.BatchNumber.Trim()))
            return Error.Conflict("WhitePlacebo.BatchNumber", "This White Placebo batch number already exists.");
        var selected = body.Inputs.Select(item => item.ShelfMaterialBatchId).ToArray();
        var shelves = await Shelves(scope.Value).Where(item => selected.Contains(item.Id)).ToListAsync();
        if (shelves.Count != selected.Length)
            return Error.Validation("WhitePlacebo.Stock", "An ingredient batch is no longer in your department's raw-material store.");
        var expected = recipe.Ingredients.ToDictionary(item => item.MaterialId);
        var grouped = shelves.GroupBy(item => item.MaterialBatch.MaterialId).ToList();
        if (grouped.Count != expected.Count || grouped.Any(group => !expected.ContainsKey(group.Key)))
            return Error.Validation("WhitePlacebo.Ingredients", "Select stock for every recipe ingredient and no other material.");
        var inputs = new List<WhitePlaceboPreparationInput>();
        foreach (var group in grouped)
        {
            var ingredient = expected[group.Key];
            var prescribed = ingredient.Quantity * body.ExpectedQuantity / recipe.ExpectedQuantity;
            var used = body.Inputs.Where(line => group.Any(shelf => shelf.Id == line.ShelfMaterialBatchId)).Sum(line => line.Quantity);
            if (used != prescribed && string.IsNullOrWhiteSpace(body.Notes))
                return Error.Validation("WhitePlacebo.Variance", "Explain any ingredient quantity that differs from the scaled approved recipe.");
            foreach (var shelf in group)
            {
                var quantity = body.Inputs.Single(line => line.ShelfMaterialBatchId == shelf.Id).Quantity;
                if (shelf.MaterialBatch.UoMId != ingredient.UoMId || shelf.MaterialBatch.Material.IsWhitePlacebo)
                    return Error.Validation("WhitePlacebo.Unit", "An ingredient's stock unit no longer matches its recipe unit.");
                var eligible = ValidateStock(shelf, quantity, DateTime.UtcNow);
                if (eligible.IsFailure) return eligible.Errors;
                inputs.Add(new WhitePlaceboPreparationInput {
                    Id = Guid.NewGuid(), MaterialId = ingredient.MaterialId, MaterialBatchId = shelf.MaterialBatchId,
                    SourceShelfId = shelf.Id, SourceWarehouseId = shelf.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.WarehouseId,
                    UoMId = ingredient.UoMId, MaterialName = ingredient.Material.Name, MaterialCode = ingredient.Material.Code,
                    BatchNumber = shelf.MaterialBatch.BatchNumber, UnitSymbol = ingredient.UoM.Symbol,
                    RecipeQuantity = prescribed, Quantity = quantity, CreatedById = currentUser.UserId });
            }
        }
        foreach (var batch in shelves.GroupBy(item => item.MaterialBatchId))
            if (inputs.Where(item => item.MaterialBatchId == batch.Key).Sum(item => item.Quantity) > batch.First().MaterialBatch.RemainingQuantity)
                return Error.Conflict("WhitePlacebo.Reserved", "The selected ingredient quantities would use stock reserved for other production.");
        var requisition = new Requisition { Id = Guid.NewGuid(), Code = $"RM/WP/{body.Id:N}",
            RequestedById = currentUser.UserId.Value, DepartmentId = scope.Value, RequisitionType = RequisitionType.Stock,
            Status = RequestStatus.New, Comments = $"White Placebo batch {body.BatchNumber.Trim()}", Approvals = [],
            Items = inputs.GroupBy(item => new { item.MaterialId, item.UoMId }).Select(group => new RequisitionItem {
                Id = Guid.NewGuid(), MaterialId = group.Key.MaterialId, UoMId = group.Key.UoMId, Quantity = group.Sum(item => item.Quantity),
                Status = RequestStatus.New }).ToList() };
        var preparation = new WhitePlaceboPreparation { Id = body.Id, DepartmentId = scope.Value,
            MaterialId = recipe.MaterialId, UoMId = recipe.UoMId, RecipeRevision = recipe.Revision,
            RecipeReference = recipe.Reference, Instructions = recipe.Instructions, StorageConditions = recipe.StorageConditions,
            HoldHours = recipe.HoldHours, BatchNumber = body.BatchNumber.Trim(), ExpectedQuantity = body.ExpectedQuantity,
            Notes = body.Notes?.Trim() ?? "", Requisition = requisition, Inputs = inputs, CreatedById = currentUser.UserId };
        var recheck = await Department(PermissionKeys.CanRequestWhitePlaceboIngredients);
        if (recheck.IsFailure || recheck.Value != scope.Value)
            return Error.Forbidden("WhitePlacebo.Permission", "Your permissions changed before saving.");
        context.WhitePlaceboPreparations.Add(preparation);
        await context.SaveChangesAsync();
        await approvals.CreateInitialApprovalsAsync("RawStockRequisition", requisition.Id, currentUser.UserId);
        if (transaction != null) await transaction.CommitAsync();
        return preparation.Id;
    }

    private static Result ValidateStock(DOMAIN.Entities.Warehouses.ShelfMaterialBatch shelf, decimal quantity, DateTime now)
    {
        var batch = shelf.MaterialBatch;
        if (batch.Status != DOMAIN.Entities.Materials.Batch.BatchStatus.Available
            || MaterialValidity.IsExpired(batch, now)
            || quantity > shelf.Quantity || quantity > batch.RemainingQuantity)
            return Error.Conflict("WhitePlacebo.StockChanged", $"Batch {batch.BatchNumber} is unavailable, due for retest, expired or no longer has enough free stock.");
        return Result.Success();
    }
}
