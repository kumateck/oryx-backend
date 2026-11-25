using APP.Services.Authorization;
using APP.Utils;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Warehouses;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public partial class MaterialRepository
{
    public async Task<Result> MoveMaterialBatchToWarehouseFromHolding(SupplyMaterialBatchFromHoldingRequest request, Guid userId)
    {
        if (context.Database.IsRelational() && context.Database.CurrentTransaction is null)
            return await ProductionMaterialTransaction.Run(context, () => MoveMaterialBatchToWarehouseFromHolding(request, userId));
        var actor = await context.Users.AsNoTracking().FirstOrDefaultAsync(user => user.Id == userId && !user.IsDisabled);
        var holding = await context.HoldingMaterialTransfers.IgnoreQueryFilters().IgnoreAutoIncludes()
            .Include(row => row.StockTransfer)
            .Include(row => row.Batches).ThenInclude(row => row.MaterialBatch).ThenInclude(batch => batch.Material)
            .Include(row => row.Batches).ThenInclude(row => row.MaterialBatch).ThenInclude(batch => batch.UoM)
            .Include(row => row.Batches).ThenInclude(row => row.DestinationWarehouse)
            .Include(row => row.Batches).ThenInclude(row => row.SourceWarehouse)
            .Include(row => row.Batches).ThenInclude(row => row.DestinationUoM)
            .SingleOrDefaultAsync(row => row.Id == request.HoldingMaterialId && row.DeletedAt == null);
        if (actor?.DepartmentId is null || holding is null) return Error.NotFound("HoldingMaterial.NotFound", "Transfer stock not found.");
        var matches = holding.Batches.Where(row => row.MaterialBatchId == request.MaterialBatchId).ToList();
        if (matches.Count != 1) return Error.Conflict("HoldingMaterial.Batch", "The held batch is missing or ambiguous.");
        var held = matches[0]; var batch = held.MaterialBatch;
        var kind = batch.Material.Kind;
        var key = kind == MaterialKind.Raw ? PermissionKeys.CanAssignRawMaterialStockTransfers : PermissionKeys.CanAssignPackagingMaterialStockTransfers;
        if (!(await CurrentPermissionSet.Load(context, userId)).Contains(key))
            return Error.Forbidden("HoldingMaterial.Permission", "You do not have permission to receive this material transfer.");
        if (held.DestinationWarehouse.DepartmentId != actor.DepartmentId || held.DestinationWarehouse.DeletedAt.HasValue)
            return Error.Forbidden("HoldingMaterial.Department", "Only the receiving department can shelve this transfer.");
        if (holding.Status != HoldingMaterialTransferStatus.Pending || held.ReceivedAt.HasValue)
            return Error.Conflict("HoldingMaterial.Received", "This held batch has already been received.");
        var unit = held.DestinationUoM ?? batch.UoM;
        var destinationQuantity = held.DestinationQuantity ?? held.Quantity;
        if (batch.DeletedAt.HasValue || batch.Material.DeletedAt.HasValue || unit?.DeletedAt != null || batch.UoM?.DeletedAt != null
            || batch.Status != BatchStatus.Available || MaterialValidity.IsExpired(batch, DateTime.UtcNow))
            return Error.Conflict("HoldingMaterial.Quality", "The held lot is no longer available and within its approved validity period.");
        var moves = request.ShelfMaterialBatches;
        if (unit is null || destinationQuantity <= 0 || held.Quantity <= 0 || moves is null || moves.Count == 0
            || moves.Any(move => move.Quantity <= 0 || move.UomId != unit.Id)
            || moves.Sum(move => move.Quantity) != destinationQuantity
            || moves.Select(move => move.WarehouseLocationShelfId).Distinct().Count() != moves.Count)
            return Error.Validation("HoldingMaterial.Quantity", "Place the whole held batch exactly once, using its saved receiving unit.");
        if (batch.TotalQuantity - batch.ConsumedQuantity - batch.QuantityAssigned < held.Quantity)
            return Error.Conflict("HoldingMaterial.Balance", "The lot's unassigned balance does not cover its held quantity. Reconcile stock first.");
        var shelfIds = moves.Select(move => move.WarehouseLocationShelfId).ToList();
        var expectedType = kind == MaterialKind.Raw ? WarehouseType.RawMaterialStorage : WarehouseType.PackagedStorage;
        var shelves = await context.WarehouseLocationShelves.IgnoreQueryFilters().IgnoreAutoIncludes()
            .Where(shelf => shelfIds.Contains(shelf.Id) && shelf.DeletedAt == null && shelf.WarehouseLocationRack.DeletedAt == null
                && shelf.WarehouseLocationRack.WarehouseLocation.DeletedAt == null
                && shelf.WarehouseLocationRack.WarehouseLocation.WarehouseId == held.DestinationWarehouseId
                && shelf.WarehouseLocationRack.WarehouseLocation.Warehouse.Type == expectedType).ToListAsync();
        if (shelves.Count != shelfIds.Count) return Error.Validation("HoldingMaterial.Shelf", "Choose active shelves in the receiving warehouse of the correct material kind.");
        var existing = await context.ShelfMaterialBatches.Where(row => row.MaterialBatchId == batch.Id && shelfIds.Contains(row.WarehouseLocationShelfId)).ToListAsync();
        if (existing.Any(row => row.UoMId != null && row.UoMId != batch.UoMId) || existing.GroupBy(row => row.WarehouseLocationShelfId).Any(group => group.Count() > 1))
            return Error.Conflict("HoldingMaterial.ShelfUnit", "Reconcile duplicate or inconsistent stock on the receiving shelves first.");
        var receivedAt = DateTime.UtcNow;
        var ledger = await StockTransferLedger.Prepare(context, [new(batch, held.DestinationWarehouseId, held.Quantity, 0,
            $"Stock transfer {holding.StockTransfer?.Code ?? holding.Id.ToString()}: received from {held.SourceWarehouse.Name}",
            userId, receivedAt, held.DensityGramsPerMilliliter)]);
        if (ledger.IsFailure) return ledger.Error;
        if (!(await CurrentPermissionSet.Load(context, userId)).Contains(key))
            return Error.Forbidden("HoldingMaterial.Permission", "Your permission to receive this material transfer was removed.");
        var remaining = held.Quantity;
        for (var i = 0; i < moves.Count; i++)
        {
            var move = moves[i];
            // Preserve the lot's exact physical quantity; assign any decimal division remainder to the last shelf.
            var physical = i == moves.Count - 1 ? remaining : held.Quantity * move.Quantity / destinationQuantity;
            remaining -= physical;
            var target = existing.SingleOrDefault(row => row.WarehouseLocationShelfId == move.WarehouseLocationShelfId);
            if (target is null)
            {
                target = new ShelfMaterialBatch { MaterialBatchId = batch.Id, WarehouseLocationShelfId = move.WarehouseLocationShelfId, UoMId = batch.UoMId };
                context.ShelfMaterialBatches.Add(target);
            }
            target.Quantity += physical; target.Note = move.Note;
        }
        batch.QuantityAssigned += held.Quantity;
        held.ReceivedAt = receivedAt; held.ReceivedById = userId;
        if (holding.Batches.All(row => row.ReceivedAt.HasValue)) holding.Status = HoldingMaterialTransferStatus.Transferred;
        context.MassMaterialBatchMovements.Add(new() { BatchId = batch.Id, FromWarehouseId = held.SourceWarehouseId,
            ToWarehouseId = held.DestinationWarehouseId, Quantity = held.Quantity, MovedAt = held.ReceivedAt.Value, MovedById = userId });
        context.MaterialBatchEvents.Add(new() { BatchId = batch.Id, Quantity = held.Quantity, Type = EventType.Moved, UserId = userId });
        context.BinCardInformation.AddRange(ledger.Value);
        await context.SaveChangesAsync();
        return Result.Success();
    }
}
