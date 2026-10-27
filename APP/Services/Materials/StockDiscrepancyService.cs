using System.Data;
using APP.IRepository;
using DOMAIN.Entities.BinCards;
using DOMAIN.Entities.InventoryLedgers;
using DOMAIN.Entities.StockAdjustments;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Services.Materials;

public static class StockDiscrepancyService
{
    public static async Task<Result<Guid>> Create(ApplicationDbContext context,
        IApprovalRepository approvals, CreateStockDiscrepancy request, Guid actorId)
    {
        if (request.ShelfMaterialBatchId == Guid.Empty || request.PhysicalQuantity < 0 ||
            !Enum.IsDefined(request.ReasonCode) || string.IsNullOrWhiteSpace(request.Notes) ||
            request.Notes.Length > 10000)
            return Error.Validation("StockDiscrepancy.Invalid", "Batch, physical count, reason and notes are required.");
        if (!await context.Approvals.AnyAsync(item => item.ItemType == nameof(StockDiscrepancy)
            && item.ApprovalStages.Any(stage => stage.Required)))
            return Error.Validation("Approval.Required", "Configure required StockDiscrepancy approval stages first.");
        await using var transaction = context.Database.IsRelational()
            ? await context.Database.BeginTransactionAsync(IsolationLevel.Serializable) : null;
        var shelf = await context.ShelfMaterialBatches.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == request.ShelfMaterialBatchId);
        if (shelf is null) return Error.NotFound("Batch.NotFound", "Shelf material batch not found.");
        if (shelf.Quantity == request.PhysicalQuantity)
            return Error.Validation("StockDiscrepancy.NoVariance", "Physical and recorded stock are equal.");
        if (await context.StockDiscrepancies.AnyAsync(item =>
            item.ShelfMaterialBatchId == shelf.Id && !item.Rejected && !item.ReconciledAt.HasValue))
            return Error.Validation("StockDiscrepancy.Active", "An unresolved report exists for this shelf batch.");

        var report = new StockDiscrepancy
        {
            ShelfMaterialBatchId = shelf.Id,
            SystemQuantitySnapshot = shelf.Quantity,
            PhysicalQuantity = request.PhysicalQuantity,
            ReasonCode = request.ReasonCode,
            Notes = request.Notes.Trim(),
            CreatedById = actorId,
            CreatedAt = DateTime.UtcNow,
        };
        context.StockDiscrepancies.Add(report);
        await context.SaveChangesAsync();
        await approvals.CreateInitialApprovalsAsync(nameof(StockDiscrepancy), report.Id, actorId);
        if (transaction is not null) await transaction.CommitAsync();
        return report.Id;
    }

    public static async Task<Result> Reconcile(ApplicationDbContext context, Guid id, Guid actorId)
    {
        await using var transaction = context.Database.IsRelational()
            ? await context.Database.BeginTransactionAsync(IsolationLevel.Serializable) : null;
        var report = await context.StockDiscrepancies.SingleOrDefaultAsync(item => item.Id == id);
        if (report is null) return Error.NotFound("StockDiscrepancy.NotFound", "Report not found.");
        if (report.ReconciledAt.HasValue)
            return Error.Validation("StockDiscrepancy.Reconciled", "This report was already reconciled.");
        if (!report.Approved || report.Rejected)
            return Error.Validation("StockDiscrepancy.Approval", "Approval is required before reconciliation.");

        var shelf = await context.ShelfMaterialBatches
            .Include(item => item.MaterialBatch).ThenInclude(item => item.ReservedQuantities)
            .Include(item => item.WarehouseLocationShelf)
                .ThenInclude(item => item.WarehouseLocationRack)
                    .ThenInclude(item => item.WarehouseLocation)
            .SingleOrDefaultAsync(item => item.Id == report.ShelfMaterialBatchId);
        if (shelf is null) return Error.NotFound("Batch.NotFound", "Shelf material batch not found.");
        if (shelf.Quantity != report.SystemQuantitySnapshot)
            return Error.Validation("StockDiscrepancy.Stale", "Stock changed since the count. Report a new count.");

        var variance = report.PhysicalQuantity - shelf.Quantity;
        if (shelf.MaterialBatch.TotalQuantity + variance <
            shelf.MaterialBatch.ConsumedQuantity + shelf.MaterialBatch.ReservedQuantity +
                shelf.MaterialBatch.QuantityAssigned)
            return Error.Validation("StockDiscrepancy.Total",
                "Reconciliation would reduce batch stock below consumed, reserved or assigned quantity.");
        shelf.Quantity = report.PhysicalQuantity;
        shelf.MaterialBatch.TotalQuantity += variance;
        var now = DateTime.UtcNow;
        context.BinCardInformation.Add(new BinCardInformation
        {
            MaterialBatchId = shelf.MaterialBatchId,
            WarehouseId = shelf.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.WarehouseId,
            QuantityReceived = Math.Max(variance, 0),
            QuantityIssued = Math.Max(-variance, 0),
            BalanceQuantity = shelf.Quantity,
            UoMId = shelf.UoMId,
            Description = $"Approved stock discrepancy {report.Id}",
            CreatedAt = now,
            CreatedById = actorId,
        });
        context.InventoryLedgers.Add(new InventoryLedger
        {
            TransactionType = "Discrepancy",
            ReferenceId = report.Id.ToString(),
            ShelfMaterialBatchId = shelf.Id,
            ChangeAmount = variance,
            PostTransactionBalance = shelf.Quantity,
            Notes = report.Notes,
            CreatedAt = now,
            CreatedById = actorId,
        });
        report.ReconciledAt = now;
        report.ReconciledById = actorId;
        await context.SaveChangesAsync();
        if (transaction is not null) await transaction.CommitAsync();
        return Result.Success();
    }

    public static IQueryable<StockDiscrepancyDto> Query(ApplicationDbContext context) =>
        context.StockDiscrepancies.AsNoTracking().Select(item => new StockDiscrepancyDto
        {
            Id = item.Id,
            ShelfMaterialBatchId = item.ShelfMaterialBatchId,
            BatchNumber = item.ShelfMaterialBatch.MaterialBatch.BatchNumber,
            MaterialName = item.ShelfMaterialBatch.MaterialBatch.Material.Name,
            Unit = item.ShelfMaterialBatch.UoM.Symbol,
            SystemQuantitySnapshot = item.SystemQuantitySnapshot,
            PhysicalQuantity = item.PhysicalQuantity,
            Variance = item.PhysicalQuantity - item.SystemQuantitySnapshot,
            ReasonCode = item.ReasonCode,
            Notes = item.Notes,
            Approved = item.Approved,
            Rejected = item.Rejected,
            ReconciledAt = item.ReconciledAt,
            CreatedAt = item.CreatedAt,
        });
}
