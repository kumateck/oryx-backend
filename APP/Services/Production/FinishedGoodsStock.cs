using APP.Services.Authorization;
using System.Data;
using DOMAIN.Entities.BinCards;
using DOMAIN.Entities.InventoryLedgers;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Products.Production;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SHARED;

namespace APP.Services.Production;

/// <summary>One stock basis for receipt, adjustment and departure. All quantities are yield.</summary>
public static class FinishedGoodsStock
{
    public static async Task<Result> Write(ApplicationDbContext db, string key, Guid? actor, Func<Task<Result>> action)
    {
        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable) : null;
        try
        {
            var denied = await Require(db, key, actor);
            if (denied is not null) return denied;
            var result = await action();
            if (result.IsSuccess && transaction is not null) await transaction.CommitAsync();
            if (result.IsFailure) db.ChangeTracker.Clear();
            return result;
        }
        catch (DbUpdateConcurrencyException) { db.ChangeTracker.Clear(); return Changed; }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: "40001" or "40P01" or "23505" })
        { db.ChangeTracker.Clear(); return Changed; }
        catch (PostgresException e) when (e.SqlState is "40001" or "40P01" or "23505")
        { db.ChangeTracker.Clear(); return Changed; }
    }
    public static async Task<Result<T>> Write<T>(ApplicationDbContext db, string key, Guid? actor, Func<Task<Result<T>>> action)
    {
        Result<T> value = null;
        var result = await Write(db, key, actor, async () =>
        {
            value = await action();
            return value.IsSuccess ? Result.Success() : Result.Failure(value.Error);
        });
        return result.IsSuccess ? value : result.Error;
    }
    public static Error Changed => Error.Conflict("FinishedGoods.Changed", "Stock or documents changed. Refresh and try again.");
    public static async Task<Error> Require(ApplicationDbContext db, string key, Guid? actor = null) =>
        (await CurrentPermissionSet.Load(db, actor ?? db.CurrentActorId)).Contains(key)
            ? null : Error.Forbidden("FinishedGoods.Permission", $"You need {key} for this action.");

    public static Error Eligible(FinishedGoodsTransferNote note)
    {
        if (note is null) return Error.Validation("FinishedGoods.SourceMissing", "The original batch stock record is unavailable. Reconcile the source before progression.");
        if (note.RequiresStockReconciliation)
            return Error.Validation("FinishedGoods.ReconciliationRequired", "This historical stock needs an evidenced reconciliation before it can progress.");
        var batch = note.BatchManufacturingRecord;
        if (!note.Approved || !note.IsApproved)
            return Error.Validation("FinishedGoods.NotReleased", "The transfer must be approved and received first.");
        if (note.IsOnHold || batch?.Status is not (BatchManufacturingStatus.Approved or BatchManufacturingStatus.Available))
            return Error.Validation("FinishedGoods.QualityHold", "This batch is not released by quality or is on hold.");
        if (batch.ExpiryDate == null || batch.ExpiryDate.Value.Date <= DateTime.UtcNow.Date)
            return Error.Validation("FinishedGoods.Expiry", "A valid expiry date is required; expired stock cannot progress.");
        return null;
    }

    public static async Task Post(ApplicationDbContext db, FinishedGoodsTransferNote note, decimal change,
        string type, string reference, Guid actor, string description, string waybill = null)
    {
        // The caller updates the canonical note before posting. The bin card is a product/batch
        // balance, not a sum of sent transfers or an individual note's balance.
        var batchNotes = await db.FinishedGoodsTransferNotes.IgnoreAutoIncludes().Where(n => n.BatchManufacturingRecordId == note.BatchManufacturingRecordId).ToListAsync();
        var balance = batchNotes.Sum(n => n.PhysicalQuantity);
        db.ProductBinCardInformation.Add(new ProductBinCardInformation
        {
            BatchId = note.BatchManufacturingRecordId, UoMId = note.UoMId, Description = description.Length <= 500 ? description : description[..500],
            WayBill = waybill, QuantityReceived = Math.Max(0, change), QuantityIssued = Math.Max(0, -change),
            BalanceQuantity = balance, CreatedById = actor, CreatedAt = DateTime.UtcNow,
        });
        db.InventoryLedgers.Add(new InventoryLedger
        {
            TransactionType = type, ReferenceId = reference, FinishedGoodsTransferNoteId = note.Id,
            ChangeAmount = change, PostTransactionBalance = note.PhysicalQuantity,
            Notes = description.Length <= 1000 ? description : description[..1000], CreatedById = actor, CreatedAt = DateTime.UtcNow,
        });
        var productId = note.BatchManufacturingRecord?.ProductionScheduleProduct?.ProductId
            ?? await db.BatchManufacturingRecords.Where(b => b.Id == note.BatchManufacturingRecordId)
                .Select(b => b.ProductionScheduleProduct.ProductId).SingleAsync();
        if (change != 0) db.FinishedProductBatchMovements.Add(new FinishedProductBatchMovement
        {
            ProductId = productId, Quantity = Math.Abs(change), MovedAt = DateTime.UtcNow, MovedById = actor,
            FromWarehouseId = change < 0 ? note.ToWarehouseId : type == "FgReceipt" ? note.FromWarehouseId : null,
            ToWarehouseId = change > 0 ? note.ToWarehouseId : null,
        });
    }
}
