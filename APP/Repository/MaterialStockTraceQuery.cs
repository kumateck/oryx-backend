using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

internal static class MaterialStockTraceQuery
{
    private sealed class UnitException : Exception { }

    internal static async Task<Result<MaterialStockTraceDto>> Load(ApplicationDbContext db, Material material, Guid departmentId, DateTime start, DateTime now)
    {
        var warehouses = await db.Warehouses.IgnoreQueryFilters().AsNoTracking().Where(row => row.DepartmentId == departmentId)
            .Select(row => new { row.Id, row.Name, row.Type, row.DeletedAt }).ToListAsync();
        var type = material.Kind == MaterialKind.Raw ? WarehouseType.RawMaterialStorage : WarehouseType.PackagedStorage;
        var stores = warehouses.Where(row => row.Type == type).Select(row => row.Id).ToList();
        var floors = warehouses.Where(row => row.Type == WarehouseType.Production).Select(row => row.Id).ToList();
        var warehouseIds = warehouses.Select(row => row.Id).ToList();
        var department = await db.Departments.IgnoreQueryFilters().AsNoTracking().Where(row => row.Id == departmentId)
            .Select(row => row.Name).SingleAsync();
        var links = await db.MaterialDepartments.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
            .Where(row => row.MaterialId == material.Id && row.DepartmentId == departmentId && row.DeletedAt == null && row.UoM.DeletedAt == null)
            .Select(row => row.UoM.Symbol).Distinct().ToListAsync();
        var lotUnits = await db.MaterialBatches.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
            .Where(row => row.MaterialId == material.Id).Select(row => row.UoM.Symbol).Distinct().ToListAsync();
        if (links.Count > 1 || links.Count == 0 && lotUnits.Count > 1)
            return Error.Conflict("StockTrace.Unit", "Choose one receiving unit on this material's department link before reconciling its stock.");
        var unit = links.SingleOrDefault() ?? lotUnits.SingleOrDefault();
        var density = await MaterialDensity.Load(db, material.Id);
        var category = await db.MaterialCategories.IgnoreQueryFilters().AsNoTracking().Where(row => row.Id == material.MaterialCategoryId).Select(row => row.Name).FirstOrDefaultAsync();
        var result = new MaterialStockTraceDto { MaterialId = material.Id, MaterialCode = material.Code, MaterialName = material.Name,
            MaterialKind = material.Kind, Category = category, Pharmacopoeia = material.Pharmacopoeia,
            DepartmentId = departmentId, DepartmentName = department, Unit = unit, PeriodStart = start, GeneratedAt = now };
        decimal Convert(decimal quantity, string source) => quantity == 0 ? 0 : MaterialDensity.Convert(quantity, source, unit, density) ?? throw new UnitException();
        try
        {
            var shelves = await db.ShelfMaterialBatches.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
                .Where(row => row.DeletedAt == null && row.Quantity > 0 && row.MaterialBatch.MaterialId == material.Id
                    && stores.Contains(row.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.WarehouseId))
                .Select(row => new { row.Id, row.MaterialBatchId, row.MaterialBatch.BatchNumber, row.Quantity,
                    Unit = row.MaterialBatch.UoM.Symbol, WarehouseId = row.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.WarehouseId,
                    Warehouse = row.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.Warehouse.Name,
                    Shelf = row.WarehouseLocationShelf.Code ?? row.WarehouseLocationShelf.Name, row.CreatedAt }).ToListAsync();
            foreach (var shelf in shelves)
                result.Rows.Add(new() { Id = shelf.Id, MaterialBatchId = shelf.MaterialBatchId, MaterialBatchNumber = shelf.BatchNumber,
                    State = MaterialStockTraceState.Store, Quantity = Convert(shelf.Quantity, shelf.Unit), Unit = unit,
                    Warehouse = shelf.Warehouse, Shelf = shelf.Shelf, OccurredAt = shelf.CreatedAt, Evidence = "Shelf stock" });
            result.StoreStock = result.Rows.Sum(row => row.Quantity);
            var unissued = ProductionMaterialAllocator.UnissuedReservations(db);
            var holds = await unissued.IgnoreAutoIncludes().AsNoTracking().Where(row => row.MaterialBatch.MaterialId == material.Id
                    && (row.WarehouseLocationShelfId.HasValue
                        ? stores.Contains(row.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.WarehouseId)
                        : row.Warehouse.DepartmentId == departmentId))
                .Select(row => new { row.Id, row.MaterialBatchId, row.MaterialBatch.BatchNumber, row.Quantity, Unit = row.UoM.Symbol,
                    Warehouse = row.WarehouseLocationShelfId.HasValue ? row.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.Warehouse.Name : row.Warehouse.Name,
                    Shelf = row.WarehouseLocationShelfId.HasValue ? row.WarehouseLocationShelf.Code ?? row.WarehouseLocationShelf.Name : null,
                    Product = row.ProductionScheduleProduct.Product.Name, ProductBatch = row.ProductionScheduleProduct.BatchNumber,
                    Schedule = row.ProductionScheduleProduct.ProductionSchedule.Code, row.CreatedAt }).ToListAsync();
            foreach (var hold in holds)
                result.Rows.Add(new() { Id = hold.Id, MaterialBatchId = hold.MaterialBatchId, MaterialBatchNumber = hold.BatchNumber,
                    State = MaterialStockTraceState.Reserved, Quantity = Convert(hold.Quantity, hold.Unit), Unit = unit,
                    Warehouse = hold.Warehouse, Shelf = hold.Shelf, Product = hold.Product, ProductBatchNumber = hold.ProductBatch,
                    Reference = hold.Schedule, OccurredAt = hold.CreatedAt, Evidence = "Unissued production reservation" });
            result.ReservedStock = holds.Sum(row => Convert(row.Quantity, row.Unit));
            foreach (var store in stores)
            {
                var measured = (await StoreStock.Measure(db, store, new Dictionary<Guid, (string?, decimal?)> { [material.Id] = (unit, density) }))
                    .GetValueOrDefault(material.Id, StoreStockLevel.None);
                result.FreeStock += measured.Free; result.ExpiredStock += measured.Expired; result.UnavailableStock += measured.Unavailable;
            }
            if (result.StoreStock != result.ReservedStock + result.FreeStock + result.ExpiredStock + result.UnavailableStock)
                result.Gaps.Add("Store stock does not equal live reservations plus free, expired and unavailable stock. Review shelf units and overallocated holds.");
            // The same ledger the bin card shows: store warehouses of the kind, balances replayed in date order.
            var ledger = await MaterialStoreLedger.Load(db, material.Id, material.Kind, departmentId);
            if (ledger.IsFailure) return ledger.Error;
            var entries = ledger.Value.Entries;
            var cardIds = entries.Select(row => row.Id).ToList();
            var cards = await db.BinCardInformation.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
                .Where(row => cardIds.Contains(row.Id))
                .Select(row => new { row.Id, row.MaterialBatch.BatchNumber, row.Description, row.ArNumber, Warehouse = row.Warehouse.Name,
                    Reference = row.RequisitionCode ?? row.WayBill, Product = row.Product.Name, row.ProductBatchNumber })
                .ToDictionaryAsync(row => row.Id);
            result.OpeningBalance = entries.Where(row => row.At < start).Sum(row => row.Received - row.Issued);
            result.Received = entries.Where(row => row.At >= start).Sum(row => row.Received);
            result.Issued = entries.Where(row => row.At >= start).Sum(row => row.Issued);
            result.LedgerClosing = result.OpeningBalance + result.Received - result.Issued;
            result.LedgerRows = entries.Select(row => new MaterialStockTraceLedgerRow
            {
                Id = row.Id, MaterialBatchId = row.MaterialBatchId, MaterialBatchNumber = cards[row.Id].BatchNumber, OccurredAt = row.At,
                Description = cards[row.Id].Description, Reference = cards[row.Id].Reference, ArNumber = cards[row.Id].ArNumber,
                Warehouse = cards[row.Id].Warehouse, Product = cards[row.Id].Product, ProductBatchNumber = cards[row.Id].ProductBatchNumber,
                Unit = unit, Received = row.Received, Issued = row.Issued, Balance = row.Balance,
            }).ToList();
            result.LedgerGap = result.LedgerClosing - result.StoreStock;
            if (result.LedgerGap != 0) result.Gaps.Add("The store bin card and shelf stock disagree. Review missing receipts, issues and unit conversions; no stock has been adjusted.");
            var consumed = await db.MaterialBatchEvents.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
                .Where(row => row.DeletedAt == null && row.Type == EventType.Consumed && row.Batch.MaterialId == material.Id
                    && row.ConsumptionWarehouseId.HasValue && warehouseIds.Contains(row.ConsumptionWarehouseId.Value))
                .Select(row => new { row.Id, BatchId = row.BatchId, row.Batch.BatchNumber, row.Quantity, Unit = row.Batch.UoM.Symbol,
                    WarehouseId = row.ConsumptionWarehouseId.Value, Warehouse = row.ConsumptionWarehouse.Name,
                    At = row.ConsumedAt ?? row.CreatedAt, Actor = row.User.FirstName + " " + row.User.LastName,
                    Product = row.ProductionScheduleProduct.Product.Name, ProductBatch = row.ProductionScheduleProduct.BatchNumber,
                    Schedule = row.ProductionScheduleProduct.ProductionSchedule.Code }).ToListAsync();
            foreach (var item in consumed)
                result.Rows.Add(new() { Id = item.Id, MaterialBatchId = item.BatchId, MaterialBatchNumber = item.BatchNumber,
                    State = MaterialStockTraceState.Consumed, Quantity = Convert(item.Quantity, item.Unit), Unit = unit,
                    Warehouse = item.Warehouse, OccurredAt = item.At, Actor = item.Actor, Reference = item.Schedule ?? item.Id.ToString(),
                    Product = item.Product, ProductBatchNumber = item.ProductBatch, Evidence = "Recorded consumption event" });
            result.ConsumedStock = consumed.Sum(row => Convert(row.Quantity, row.Unit));
            var movements = await db.MassMaterialBatchMovements.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
                .Where(row => row.DeletedAt == null && row.Batch.MaterialId == material.Id
                    && (row.FromWarehouseId.HasValue && floors.Contains(row.FromWarehouseId.Value) || row.ToWarehouseId.HasValue && floors.Contains(row.ToWarehouseId.Value)))
                .Select(row => new { row.Id, row.BatchId, row.Batch.BatchNumber, row.Quantity, Unit = row.Batch.UoM.Symbol,
                    row.FromWarehouseId, row.ToWarehouseId, row.MovedAt }).ToListAsync();
            var allocated = await db.MaterialBatchReservedQuantities.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
                .Where(row => row.MaterialBatch.MaterialId == material.Id && row.ReleasedAt == null
                    && (row.DeletedAt == null || db.MaterialReturnNoteFullReturns.IgnoreQueryFilters().Any(line => line.DeletedAt == null
                        && line.MaterialReturnNote.DeletedAt == null && !line.Returned && line.MaterialBatchReservedQuantityId == row.Id))
                    && floors.Contains(row.WarehouseId) && !unissued.Any(hold => hold.Id == row.Id))
                .Select(row => new { row.Id, row.MaterialBatchId, row.WarehouseId, row.Quantity, Unit = row.UoM.Symbol,
                    Product = row.ProductionScheduleProduct.Product.Name, Batch = row.ProductionScheduleProduct.BatchNumber,
                    Schedule = row.ProductionScheduleProduct.ProductionSchedule.Code,
                    PendingReturn = db.MaterialReturnNoteFullReturns.IgnoreQueryFilters().Any(line => line.DeletedAt == null
                        && line.MaterialReturnNote.DeletedAt == null && !line.Returned && line.MaterialBatchReservedQuantityId == row.Id) }).ToListAsync();
            foreach (var floor in warehouses.Where(row => floors.Contains(row.Id)))
                foreach (var batchId in movements.Where(row => row.FromWarehouseId == floor.Id || row.ToWarehouseId == floor.Id).Select(row => row.BatchId)
                    .Concat(consumed.Where(row => row.WarehouseId == floor.Id).Select(row => row.BatchId))
                    .Concat(allocated.Where(row => row.WarehouseId == floor.Id).Select(row => row.MaterialBatchId)).Distinct())
                {
                    var group = movements.Where(row => row.BatchId == batchId && (row.FromWarehouseId == floor.Id || row.ToWarehouseId == floor.Id)).ToList();
                    var batchNumber = group.Select(row => row.BatchNumber).FirstOrDefault()
                        ?? consumed.Where(row => row.BatchId == batchId).Select(row => row.BatchNumber).FirstOrDefault();
                    var quantity = group.Sum(row => (row.ToWarehouseId == floor.Id ? Convert(row.Quantity, row.Unit) : 0) - (row.FromWarehouseId == floor.Id ? Convert(row.Quantity, row.Unit) : 0))
                        - consumed.Where(row => row.BatchId == batchId && row.WarehouseId == floor.Id).Sum(row => Convert(row.Quantity, row.Unit));
                    var references = allocated.Where(row => row.MaterialBatchId == batchId && row.WarehouseId == floor.Id).ToList();
                    var agrees = quantity >= 0 && quantity == references.Sum(row => Convert(row.Quantity, row.Unit));
                    if (!agrees)
                        result.Gaps.Add($"{floor.Name}, lot {batchNumber ?? batchId.ToString()}: movement balance and active issued allocations disagree. Review returns and consumption evidence.");
                    if (quantity == 0) continue;
                    if (agrees)
                    {
                        foreach (var reference in references)
                            result.Rows.Add(new() { Id = reference.Id, MaterialBatchId = batchId, MaterialBatchNumber = batchNumber,
                                State = MaterialStockTraceState.ProductionFloor, Quantity = Convert(reference.Quantity, reference.Unit), Unit = unit, Warehouse = floor.Name,
                                Reference = reference.Schedule, Product = reference.Product, ProductBatchNumber = reference.Batch,
                                OccurredAt = group.Max(row => row.MovedAt), Evidence = reference.PendingReturn
                                    ? "Full return awaiting warehouse receipt and shelving" : "Issued allocation reconciled to floor movements and consumption" });
                        continue;
                    }
                    result.Rows.Add(new() { Id = batchId, MaterialBatchId = batchId, MaterialBatchNumber = batchNumber,
                        State = MaterialStockTraceState.ProductionFloor, Quantity = quantity, Unit = unit, Warehouse = floor.Name,
                        Reference = string.Join(", ", references.Select(row => row.Schedule).Distinct()), Product = string.Join(", ", references.Select(row => row.Product).Distinct()),
                        ProductBatchNumber = string.Join(", ", references.Select(row => row.Batch).Distinct()),
                        OccurredAt = group.Select(row => (DateTime?)row.MovedAt).Max(), Evidence = "Recorded movements less returns and confirmed consumption" });
                }
            result.ProductionFloorStock = result.Rows.Where(row => row.State == MaterialStockTraceState.ProductionFloor).Sum(row => row.Quantity);
            var transit = await db.HoldingMaterialTransferBatches.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
                .Where(row => row.HoldingMaterialTransfer.DeletedAt == null && row.HoldingMaterialTransfer.Status == HoldingMaterialTransferStatus.Pending
                    && !row.ReceivedAt.HasValue && row.MaterialBatch.MaterialId == material.Id && row.DestinationWarehouse.DepartmentId == departmentId)
                .Select(row => new { row.Id, row.MaterialBatchId, row.MaterialBatch.BatchNumber, row.Quantity, Unit = row.MaterialBatch.UoM.Symbol,
                    Warehouse = row.DestinationWarehouse.Name, Reference = row.HoldingMaterialTransfer.StockTransfer.Code,
                    At = row.HoldingMaterialTransfer.CreatedAt }).ToListAsync();
            foreach (var row in transit) result.Rows.Add(new() { Id = row.Id, MaterialBatchId = row.MaterialBatchId, MaterialBatchNumber = row.BatchNumber,
                State = MaterialStockTraceState.InTransit, Quantity = Convert(row.Quantity, row.Unit), Unit = unit,
                Warehouse = row.Warehouse, Reference = row.Reference, OccurredAt = row.At, Evidence = "Issued transfer awaiting shelf receipt" });
            result.InTransitStock = transit.Sum(row => Convert(row.Quantity, row.Unit));
            var batchIds = result.Rows.Select(row => row.MaterialBatchId).Distinct().ToList();
            var batches = await db.MaterialBatches.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking().Where(row => batchIds.Contains(row.Id))
                .Select(row => new { row.Id, row.ManufacturingDate, row.ExpiryDate, row.RetestDate,
                    ArNumber = db.MaterialSamplings.Where(sample => sample.MaterialBatchId == row.Id).OrderByDescending(sample => sample.CreatedAt).Select(sample => sample.ArNumber).FirstOrDefault()
                        ?? db.StockMigrationLines.Where(line => line.ImportedBatchId == row.Id && line.StockMigration.Status == DOMAIN.Entities.Materials.StockMigrations.StockMigrationStatus.Approved)
                            .OrderByDescending(line => line.CreatedAt).Select(line => line.ArNumber).FirstOrDefault() }).ToListAsync();
            foreach (var row in result.Rows)
            {
                var batch = batches.Single(item => item.Id == row.MaterialBatchId);
                row.ArNumber = batch.ArNumber; row.ManufacturingDate = batch.ManufacturingDate; row.ExpiryDate = batch.ExpiryDate;
                row.EffectiveExpiryDate = MaterialValidity.EffectiveDate(new MaterialBatch { ExpiryDate = batch.ExpiryDate, RetestDate = batch.RetestDate }, now);
            }
            if (warehouses.Any(row => row.DeletedAt.HasValue && shelves.Any(shelf => shelf.WarehouseId == row.Id)))
                result.Gaps.Add("Stock remains on a retired warehouse. Review its location before issuing it.");
            result.Rows = result.Rows.OrderBy(row => row.State).ThenBy(row => row.MaterialBatchNumber).ThenBy(row => row.OccurredAt).ThenBy(row => row.Id).ToList();
            return result;
        }
        catch (UnitException)
        {
            return Error.Conflict("StockTrace.Unit", "Some stock evidence cannot be converted to this department's unit. Reconcile the units or verified density before reporting totals.");
        }
    }
}
