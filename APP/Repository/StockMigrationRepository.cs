using APP.IRepository;
using APP.Services.Materials;
using APP.Utils;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.BinCards;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Materials.StockMigrations;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using SHARED;
using SHARED.Requests;

namespace APP.Repository;

/// <summary>
/// Opening-stock migration: a department's stock sheet is read into lines and checked against the
/// system, corrected on the verification page, and only on approval does it replace the department's
/// storage stock.
/// </summary>
public class StockMigrationRepository(ApplicationDbContext context) : IStockMigrationRepository
{
    private static readonly string[] MassUnits = ["kg", "g", "mg"];
    private static readonly string[] VolumeUnits = ["L", "ml"];

    public async Task<Result<Guid>> Upload(IFormFile file, Guid departmentId, string massUnit, string volumeUnit, Guid userId)
    {
        if (file == null || file.Length == 0)
            return UploadErrors.EmptyFile;
        if (Unit(massUnit, MassUnits) is not { } mass || Unit(volumeUnit, VolumeUnits) is not { } volume)
            return Error.Validation("StockMigration.Units", "Choose kg, g or mg for weights and L or ml for liquids.");
        if (!await context.Departments.IgnoreQueryFilters().AnyAsync(d => d.Id == departmentId && d.DeletedAt == null))
            return Error.NotFound("Department.NotFound", "The department was not found.");

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        stream.Position = 0;

        ExcelPackage.License.SetNonCommercialPersonal("Oryx");
        using var package = new ExcelPackage(stream);
        var lines = StockMigrationSheet.Read(package);
        if (lines.Count == 0)
            return Error.Validation(
                "StockMigration.Empty",
                "No tab in the file has a material code and a quantity column with rows under them."
            );

        var migration = new StockMigration
        {
            Id = Guid.NewGuid(),
            DepartmentId = departmentId,
            FileName = file.FileName?.Length > 500 ? file.FileName[..500] : file.FileName,
            Status = StockMigrationStatus.Draft,
            MassUnit = mass,
            VolumeUnit = volume,
            CreatedById = userId,
            Lines = lines,
        };
        await Check(migration);
        context.StockMigrations.Add(migration);
        await context.SaveChangesAsync();
        return migration.Id;
    }

    public async Task<Result<List<StockMigrationDto>>> List(Guid? departmentId)
    {
        var migrations = await context
            .StockMigrations.AsNoTracking()
            .IgnoreQueryFilters()
            .Where(m => departmentId == null || m.DepartmentId == departmentId)
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new StockMigrationDto
            {
                Id = m.Id,
                DepartmentId = m.DepartmentId,
                DepartmentName = m.Department.Name,
                FileName = m.FileName,
                Status = m.Status,
                MassUnit = m.MassUnit,
                VolumeUnit = m.VolumeUnit,
                CreatedAt = m.CreatedAt,
                CreatedBy = m.CreatedBy.FirstName + " " + m.CreatedBy.LastName,
                ApprovedAt = m.ApprovedAt,
                ApprovedBy = m.ApprovedBy == null ? null : m.ApprovedBy.FirstName + " " + m.ApprovedBy.LastName,
                Total = m.Lines.Count,
                Ok = m.Lines.Count(l => l.Status == StockMigrationLineStatus.Ok),
                Warning = m.Lines.Count(l => l.Status == StockMigrationLineStatus.Warning),
                Blocked = m.Lines.Count(l => l.Status == StockMigrationLineStatus.Blocked),
                Skipped = m.Lines.Count(l => l.Status == StockMigrationLineStatus.Skipped),
                Excluded = m.Lines.Count(l => l.Status == StockMigrationLineStatus.Excluded),
                WrittenOff = m.WriteOffs.Count,
            })
            .ToListAsync();
        return migrations;
    }

    public async Task<Guid?> DepartmentOf(Guid id) =>
        await context.StockMigrations.Where(m => m.Id == id).Select(m => (Guid?)m.DepartmentId).FirstOrDefaultAsync();

    public async Task<Result<StockMigrationDto>> Get(Guid id)
    {
        var list = await context
            .StockMigrations.AsNoTracking()
            .Where(m => m.Id == id)
            .Select(m => m.DepartmentId)
            .ToListAsync();
        if (list.Count == 0)
            return NotFound;

        var all = await List(list[0]);
        var dto = all.Value.First(m => m.Id == id);
        dto.Sheets = await context
            .StockMigrationLines.Where(l => l.StockMigrationId == id)
            .Select(l => l.Sheet)
            .Distinct()
            .ToListAsync();

        var kinds = await LoadedKinds(id);
        var stores = await Stores(dto.DepartmentId);
        dto.Warehouses = stores
            .Select(w => new StockMigrationWarehouseDto
            {
                Id = w.Id,
                Name = w.Name,
                Type = w.Type,
                WillBeWiped = kinds.Contains(KindOf(w.Type)),
                Shelves = w.Locations.SelectMany(l => l.Racks).SelectMany(r => r.Shelves)
                    .OrderBy(s => s.Name)
                    .Select(s => new StockMigrationShelfDto { Id = s.Id, Name = s.Name, Code = s.Code })
                    .ToList(),
            })
            .ToList();
        return dto;
    }

    public async Task<Result<Paginateable<IEnumerable<StockMigrationLineDto>>>> GetLines(Guid id, StockMigrationLineFilter filter)
    {
        if (!await context.StockMigrations.AnyAsync(m => m.Id == id))
            return NotFound;

        var query = context.StockMigrationLines.AsNoTracking().Where(l => l.StockMigrationId == id);
        if (filter.Status.HasValue)
            query = query.Where(l => l.Status == filter.Status);
        if (!string.IsNullOrWhiteSpace(filter.Sheet))
            query = query.Where(l => l.Sheet == filter.Sheet);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = $"%{filter.Search.Trim()}%";
            query = query.Where(l =>
                EF.Functions.ILike(l.MaterialCode, term)
                || EF.Functions.ILike(l.SheetMaterialName, term)
                || EF.Functions.ILike(l.BatchNumber, term)
                || EF.Functions.ILike(l.Grn, term)
            );
        }

        var lines = await query.OrderBy(l => l.Sheet).ThenBy(l => l.RowNumber).ToListAsync();
        var materialIds = lines.Where(l => l.ResolvedMaterialId.HasValue).Select(l => l.ResolvedMaterialId.Value).Distinct().ToList();
        var materials = await context.Materials.IgnoreQueryFilters().Where(m => materialIds.Contains(m.Id)).ToDictionaryAsync(m => m.Id);
        var shelfIds = lines.Where(l => l.ResolvedShelfId.HasValue).Select(l => l.ResolvedShelfId.Value).Distinct().ToList();
        var shelves = await context
            .WarehouseLocationShelves.IgnoreQueryFilters()
            .Where(s => shelfIds.Contains(s.Id))
            .Select(s => new { s.Id, s.Name, Warehouse = s.WarehouseLocationRack.WarehouseLocation.Warehouse.Name })
            .ToDictionaryAsync(s => s.Id);
        var openCodes = lines.Where(l => l.MaterialId == null && !string.IsNullOrEmpty(l.MaterialCode)).Select(l => l.MaterialCode).Distinct().ToList();
        var shared = (await context.Materials.Where(m => openCodes.Contains(m.Code.ToUpper())).ToListAsync())
            .GroupBy(m => m.Code.Trim().ToUpperInvariant())
            .Where(g => g.Count() > 1)
            .ToDictionary(g => g.Key, g => g.Select(m => new StockMigrationMaterialChoiceDto { Id = m.Id, Code = m.Code, Name = m.Name }).ToList());
        var unitIds = lines.Where(l => l.UoMId.HasValue).Select(l => l.UoMId.Value).Distinct().ToList();
        var units = await context.UnitOfMeasures.IgnoreQueryFilters().Where(u => unitIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Symbol);

        return PaginationHelper.Paginate(
            Math.Max(filter.Page, 1),
            Math.Clamp(filter.PageSize, 1, 1000),
            lines.Select(l =>
                {
                    var material = l.ResolvedMaterialId is { } m && materials.TryGetValue(m, out var found) ? found : null;
                    var shelf = l.ResolvedShelfId is { } s && shelves.TryGetValue(s, out var sh) ? sh : null;
                    return new StockMigrationLineDto
                    {
                        Id = l.Id,
                        Sheet = l.Sheet,
                        RowNumber = l.RowNumber,
                        SheetWarehouse = l.SheetWarehouse,
                        SheetShelf = l.SheetShelf,
                        SheetMaterialCode = l.SheetMaterialCode,
                        SheetMaterialName = l.SheetMaterialName,
                        SheetBatchNumber = l.SheetBatchNumber,
                        SheetUnit = l.SheetUnit,
                        SheetQuantity = l.SheetQuantity,
                        SheetManufacturingDate = l.SheetManufacturingDate,
                        SheetExpiryDate = l.SheetExpiryDate,
                        Grn = l.Grn,
                        ArNumber = l.ArNumber,
                        MaterialCode = l.MaterialCode,
                        MaterialId = l.MaterialId,
                        WarehouseName = l.WarehouseName,
                        ShelfName = l.ShelfName,
                        ShelfId = l.ShelfId,
                        BatchNumber = l.BatchNumber,
                        UnitSymbol = l.UnitSymbol,
                        QuantityInSheet = l.QuantityInSheet,
                        ManufacturingDate = l.ManufacturingDate,
                        ExpiryDate = l.ExpiryDate,
                        Excluded = l.Excluded,
                        ResolvedMaterialId = l.ResolvedMaterialId,
                        ResolvedMaterialCode = material?.Code,
                        ResolvedMaterialName = material?.Name,
                        Kind = material?.Kind,
                        ResolvedShelfId = l.ResolvedShelfId,
                        ResolvedShelf = shelf?.Name,
                        ResolvedWarehouse = shelf?.Warehouse,
                        Quantity = l.Quantity,
                        Unit = l.UoMId is { } u && units.TryGetValue(u, out var symbol) ? symbol : null,
                        Status = l.Status,
                        Blockers = Split(l.Blockers),
                        Warnings = Split(l.Warnings),
                        Corrections = Split(l.Corrections),
                        MaterialChoices = l.MaterialId == null && l.MaterialCode != null && shared.TryGetValue(l.MaterialCode, out var choices) ? choices : [],
                    };
                })
                .ToList()
        );
    }

    public async Task<Result> UpdateLine(Guid id, Guid lineId, UpdateStockMigrationLineRequest request)
    {
        var migration = await Draft(id);
        if (migration.IsFailure)
            return migration.Error;
        var line = migration.Value.Lines.FirstOrDefault(l => l.Id == lineId);
        if (line == null)
            return Error.NotFound("StockMigration.Line", "The row was not found.");

        var batch = request.BatchNumber?.Trim() ?? string.Empty;
        if (batch != (line.BatchNumber ?? string.Empty))
            line.BatchNumberDamaged = false;
        line.MaterialCode = request.MaterialCode?.Trim().ToUpperInvariant();
        line.MaterialId = request.MaterialId;
        line.WarehouseName = request.WarehouseName?.Trim();
        line.ShelfName = request.ShelfName?.Trim();
        line.ShelfId = request.ShelfId;
        line.BatchNumber = batch;
        line.UnitSymbol = request.UnitSymbol?.Trim();
        line.QuantityInSheet = request.QuantityInSheet is > 0 ? request.QuantityInSheet : null;
        line.ManufacturingDate = Utc(request.ManufacturingDate);
        line.ExpiryDate = Utc(request.ExpiryDate);
        line.Excluded = request.Excluded;

        await Check(migration.Value);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> BulkUpdate(Guid id, BulkUpdateStockMigrationLinesRequest request)
    {
        var migration = await Draft(id);
        if (migration.IsFailure)
            return migration.Error;
        var ids = request.LineIds.ToHashSet();
        foreach (var line in migration.Value.Lines.Where(l => ids.Contains(l.Id)))
        {
            if (request.ShelfId.HasValue)
                line.ShelfId = request.ShelfId;
            if (request.Excluded.HasValue)
                line.Excluded = request.Excluded.Value;
        }
        await Check(migration.Value);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> UpdateUnits(Guid id, UpdateStockMigrationUnitsRequest request)
    {
        var migration = await Draft(id);
        if (migration.IsFailure)
            return migration.Error;
        if (Unit(request.MassUnit, MassUnits) is not { } mass || Unit(request.VolumeUnit, VolumeUnits) is not { } volume)
            return Error.Validation("StockMigration.Units", "Choose kg, g or mg for weights and L or ml for liquids.");
        migration.Value.MassUnit = mass;
        migration.Value.VolumeUnit = volume;
        await Check(migration.Value);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> Recheck(Guid id)
    {
        var migration = await Draft(id);
        if (migration.IsFailure)
            return migration.Error;
        await Check(migration.Value);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<List<StockMigrationComparisonDto>>> Comparison(Guid id)
    {
        var migration = await context.StockMigrations.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
        if (migration == null)
            return NotFound;

        var kinds = await LoadedKinds(id);
        var rows = new Dictionary<Guid, StockMigrationComparisonDto>();

        if (migration.Status == StockMigrationStatus.Approved)
        {
            var before = await context
                .StockMigrationWriteOffs.AsNoTracking()
                .IgnoreQueryFilters()
                .Where(w => w.StockMigrationId == id)
                .Select(w => new
                {
                    w.MaterialBatchId,
                    w.PreviousQuantity,
                    Material = w.ShelfMaterialBatch.MaterialBatch.Material,
                    Unit = w.ShelfMaterialBatch.UoM.Symbol,
                })
                .ToListAsync();
            foreach (var group in before.GroupBy(b => b.Material.Id))
            {
                var row = Row(rows, group.First().Material, group.First().Unit);
                row.Current = group.Sum(b => b.PreviousQuantity);
                row.CurrentBatches = group.Select(b => b.MaterialBatchId).Distinct().Count();
            }
        }
        else
        {
            var storeIds = (await Stores(migration.DepartmentId)).Where(w => kinds.Contains(KindOf(w.Type))).Select(w => w.Id).ToList();
            var current = await CurrentStock(storeIds, kinds)
                .Select(s => new { s.MaterialBatchId, s.Quantity, s.MaterialBatch.Material, Unit = s.UoM.Symbol })
                .ToListAsync();
            foreach (var group in current.GroupBy(c => c.Material.Id))
            {
                var row = Row(rows, group.First().Material, group.First().Unit);
                row.Current = group.Sum(c => c.Quantity);
                row.CurrentBatches = group.Select(c => c.MaterialBatchId).Distinct().Count();
            }
        }

        var loaded = await context
            .StockMigrationLines.AsNoTracking()
            .Where(l => l.StockMigrationId == id && l.ResolvedMaterialId != null
                && (l.Status == StockMigrationLineStatus.Ok || l.Status == StockMigrationLineStatus.Warning))
            .Select(l => new { l.ResolvedMaterialId, l.BatchNumber, l.Quantity, Unit = l.UoM.Symbol })
            .ToListAsync();
        var materialIds = loaded.Select(l => l.ResolvedMaterialId.Value).Distinct().ToList();
        var materials = await context.Materials.IgnoreQueryFilters().Where(m => materialIds.Contains(m.Id)).ToDictionaryAsync(m => m.Id);
        foreach (var group in loaded.GroupBy(l => l.ResolvedMaterialId.Value))
        {
            var row = Row(rows, materials[group.Key], group.First().Unit);
            row.New = group.Sum(l => l.Quantity ?? 0);
            row.NewBatches = group.Select(l => (l.BatchNumber ?? string.Empty).ToUpperInvariant()).Distinct().Count();
        }

        return rows.Values.OrderBy(r => r.Kind).ThenBy(r => r.Code).ToList();
    }

    public async Task<Result> Approve(Guid id, Guid userId)
    {
        var found = await Draft(id);
        if (found.IsFailure)
            return found.Error;
        var migration = found.Value;
        await Check(migration);

        var blocked = migration.Lines.Count(l => l.Status == StockMigrationLineStatus.Blocked);
        if (blocked > 0)
        {
            await context.SaveChangesAsync();
            return Error.Validation("StockMigration.Blocked", $"{blocked} row(s) still need fixing or leaving out before the stock can be approved.");
        }

        var toLoad = migration.Lines
            .Where(l => l.Status is StockMigrationLineStatus.Ok or StockMigrationLineStatus.Warning)
            .ToList();
        if (toLoad.Count == 0)
            return Error.Validation("StockMigration.Empty", "No row has stock to load.");

        var materialIds = toLoad.Select(l => l.ResolvedMaterialId!.Value).Distinct().ToList();
        var materials = await context.Materials.Where(m => materialIds.Contains(m.Id)).ToDictionaryAsync(m => m.Id);
        var kinds = materials.Values.Select(m => m.Kind).ToHashSet();
        var stores = (await Stores(migration.DepartmentId)).Where(w => kinds.Contains(KindOf(w.Type))).ToList();
        var storeIds = stores.Select(w => w.Id).ToList();
        var shelfStore = stores
            .SelectMany(w => w.Locations.SelectMany(l => l.Racks).SelectMany(r => r.Shelves).Select(s => (s.Id, w.Id)))
            .ToDictionary(x => x.Item1, x => x.Item2);
        var now = DateTime.UtcNow;
        var stamp = $"Stock migration {now:dd MMM yyyy}";

        await using var transaction = await context.Database.BeginTransactionAsync();

        // Claim the draft before touching stock: the row stays locked until commit, so a second approval made
        // at the same moment waits here and then finds it no longer a draft.
        var claimed = await context.StockMigrations
            .Where(m => m.Id == migration.Id && m.Status == StockMigrationStatus.Draft)
            .ExecuteUpdateAsync(set => set.SetProperty(m => m.Status, StockMigrationStatus.Approved));
        if (claimed == 0)
            return Error.Conflict("StockMigration.NotDraft", "This migration has already been approved or discarded.");

        // 1. Write off everything now on the department's shelves for the kinds being loaded.
        var current = await CurrentStock(storeIds, kinds)
            .Include(s => s.MaterialBatch)
            .Include(s => s.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation)
            .ToListAsync();
        var touched = current.Select(s => s.MaterialBatchId).ToHashSet();
        var elsewhere = await context
            .ShelfMaterialBatches.IgnoreQueryFilters()
            .Where(s => touched.Contains(s.MaterialBatchId) && s.Quantity > 0 && s.DeletedAt == null
                && !storeIds.Contains(s.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.WarehouseId))
            .Select(s => s.MaterialBatchId)
            .Distinct()
            .ToListAsync();

        foreach (var shelfRow in current)
        {
            var batch = shelfRow.MaterialBatch;
            context.StockMigrationWriteOffs.Add(new StockMigrationWriteOff
            {
                StockMigrationId = migration.Id,
                ShelfMaterialBatchId = shelfRow.Id,
                MaterialBatchId = batch.Id,
                PreviousQuantity = shelfRow.Quantity,
                PreviousBatchStatus = batch.Status,
            });
            context.BinCardInformation.Add(new BinCardInformation
            {
                MaterialBatchId = batch.Id,
                WarehouseId = shelfRow.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.WarehouseId,
                UoMId = shelfRow.UoMId ?? batch.UoMId,
                QuantityReceived = 0,
                QuantityIssued = shelfRow.Quantity,
                BalanceQuantity = 0,
                Description = $"{stamp}: written off, replaced by the counted stock",
            });
            shelfRow.Quantity = 0;
            shelfRow.Note = Clip($"{stamp}: written off", 1000);
            if (batch.Status == BatchStatus.Available && !elsewhere.Contains(batch.Id))
                batch.Status = BatchStatus.Consumed;
        }

        // 2. Load the counted stock as new batches.
        foreach (var group in toLoad.GroupBy(l => (Material: l.ResolvedMaterialId!.Value, Batch: l.BatchNumber.Trim().ToUpperInvariant())))
        {
            var first = group.First();
            var total = group.Sum(l => l.Quantity ?? 0);
            var batch = new MaterialBatch
            {
                Id = Guid.NewGuid(),
                MaterialId = group.Key.Material,
                BatchNumber = first.BatchNumber.Trim(),
                UoMId = first.UoMId!.Value,
                TotalQuantity = total,
                QuantityAssigned = total,
                QuantityPerContainer = total,
                NumberOfContainers = 1,
                Status = BatchStatus.Available,
                DateReceived = now,
                DateApproved = now,
                ManufacturingDate = first.ManufacturingDate,
                ExpiryDate = first.ExpiryDate,
            };
            context.MaterialBatches.Add(batch);

            foreach (var onShelf in group.GroupBy(l => l.ResolvedShelfId!.Value))
            {
                var quantity = onShelf.Sum(l => l.Quantity ?? 0);
                var grn = string.Join(", ", onShelf.Select(l => l.Grn).Where(g => !string.IsNullOrWhiteSpace(g)).Distinct());
                var ar = string.Join(", ", onShelf.Select(l => l.ArNumber).Where(a => !string.IsNullOrWhiteSpace(a)).Distinct());
                context.ShelfMaterialBatches.Add(new ShelfMaterialBatch
                {
                    WarehouseLocationShelfId = onShelf.Key,
                    MaterialBatchId = batch.Id,
                    Quantity = quantity,
                    UoMId = batch.UoMId,
                    Note = Clip($"{stamp}. GRN: {grn}. AR: {ar}", 1000),
                });
                context.BinCardInformation.Add(new BinCardInformation
                {
                    MaterialBatchId = batch.Id,
                    WarehouseId = shelfStore[onShelf.Key],
                    UoMId = batch.UoMId,
                    WayBill = Clip(grn, 500),
                    ArNumber = Clip(ar, 500),
                    QuantityReceived = quantity,
                    QuantityIssued = 0,
                    BalanceQuantity = quantity,
                    Description = $"{stamp}: opening stock",
                });
            }

            foreach (var inStore in group.GroupBy(l => shelfStore[l.ResolvedShelfId!.Value]))
            {
                context.MassMaterialBatchMovements.Add(new MassMaterialBatchMovement
                {
                    BatchId = batch.Id,
                    ToWarehouseId = inStore.Key,
                    Quantity = inStore.Sum(l => l.Quantity ?? 0),
                    MovedAt = now,
                    MovedById = userId,
                    MovementType = MovementType.ToWarehouse,
                });
            }

            foreach (var line in group)
                line.ImportedBatchId = batch.Id;
        }

        migration.Status = StockMigrationStatus.Approved;
        migration.ApprovedAt = now;
        migration.ApprovedById = userId;
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        return Result.Success();
    }

    public async Task<Result> Discard(Guid id)
    {
        var migration = await context.StockMigrations.FirstOrDefaultAsync(m => m.Id == id);
        if (migration == null)
            return NotFound;
        if (migration.Status != StockMigrationStatus.Draft)
            return Error.Conflict("StockMigration.NotDraft", "Only a migration that has not been approved can be discarded.");
        // Only while still a draft, so a discard cannot overwrite an approval that committed in between.
        var discarded = await context.StockMigrations
            .Where(m => m.Id == id && m.Status == StockMigrationStatus.Draft)
            .ExecuteUpdateAsync(set => set
                .SetProperty(m => m.Status, StockMigrationStatus.Discarded)
                .SetProperty(m => m.UpdatedAt, DateTime.UtcNow));
        return discarded == 1
            ? Result.Success()
            : Error.Conflict("StockMigration.NotDraft", "Only a migration that has not been approved can be discarded.");
    }

    /// <summary>
    /// Works out every line again from its working values: which material, shelf and unit it means, the
    /// quantity to load, and what stops or questions it. Rows are also checked against each other.
    /// </summary>
    private async Task Check(StockMigration migration)
    {
        var lines = migration.Lines;
        var department = await context.Departments.IgnoreQueryFilters().Where(d => d.Id == migration.DepartmentId).Select(d => d.Name).FirstAsync();
        var stores = await Stores(migration.DepartmentId);

        var codes = lines.Select(l => l.MaterialCode).Where(c => !string.IsNullOrEmpty(c)).Distinct().ToList();
        var picked = lines.Where(l => l.MaterialId.HasValue).Select(l => l.MaterialId.Value).Distinct().ToList();
        var candidates = await context
            .Materials.IgnoreQueryFilters()
            .Where(m => codes.Contains(m.Code.ToUpper()) || picked.Contains(m.Id))
            .ToListAsync();
        var byCode = candidates.Where(m => m.Code != null).ToLookup(m => m.Code.Trim().ToUpperInvariant());
        var byId = candidates.Where(m => !m.DeletedAt.HasValue).ToDictionary(m => m.Id);
        var materialIds = candidates.Select(m => m.Id).ToList();
        var departmentUnits = (await context
                .MaterialDepartments.Include(md => md.UoM)
                .Where(md => md.DepartmentId == migration.DepartmentId && md.DeletedAt == null && materialIds.Contains(md.MaterialId))
                .ToListAsync())
            .GroupBy(md => md.MaterialId)
            .ToDictionary(g => g.Key, g => g.First().UoM);
        var units = await context.UnitOfMeasures.ToListAsync();
        var thisMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        foreach (var line in lines)
        {
            var blockers = new List<string>();
            var warnings = new List<string>();
            line.ResolvedMaterialId = null;
            line.ResolvedShelfId = null;
            line.ResolvedWarehouseId = null;
            line.UoMId = null;
            line.Quantity = null;

            // Material
            Material material = null;
            if (line.MaterialId is { } pickedId)
            {
                if (!byId.TryGetValue(pickedId, out material))
                    blockers.Add("The material picked for this row no longer exists.");
            }
            else if (string.IsNullOrEmpty(line.MaterialCode))
                blockers.Add("No material code.");
            else
            {
                var matches = byCode[line.MaterialCode].ToList();
                var live = matches.Where(m => !m.DeletedAt.HasValue).ToList();
                if (live.Count == 1)
                {
                    material = live[0];
                    if (!string.IsNullOrWhiteSpace(line.SheetMaterialName) && LooksDifferent(line.SheetMaterialName, material.Name))
                        warnings.Add($"The sheet calls it \"{line.SheetMaterialName}\" but {material.Code} is \"{material.Name}\" in the system. Check the code.");
                }
                else if (live.Count > 1)
                    blockers.Add($"The code {line.MaterialCode} is used by {string.Join(" and ", live.Select(m => $"\"{m.Name}\""))}. Pick the right material.");
                else if (matches.Count > 0)
                    blockers.Add($"The code {line.MaterialCode} belongs to a deleted material (\"{matches[0].Name}\").");
                else
                    blockers.Add($"No material has the code {line.MaterialCode}.");
            }
            line.ResolvedMaterialId = material?.Id;
            if (material?.IsUnlimited == true)
                blockers.Add($"{material.Name} is an unlimited material and is not counted as stock; leave this row out.");

            // Shelf
            var expectedType = material == null ? (WarehouseType?)null : StoreFor(material.Kind);
            WarehouseLocationShelf shelf = null;
            Warehouse store = null;
            if (line.ShelfId is { } shelfId)
            {
                (store, shelf) = stores.SelectMany(w => ShelvesOf(w).Select(s => (w, s))).FirstOrDefault(x => x.s.Id == shelfId);
                if (shelf == null)
                    blockers.Add($"The shelf picked for this row is not in a {department} store.");
            }
            else
            {
                var named = string.IsNullOrWhiteSpace(line.WarehouseName)
                    ? stores.Where(w => expectedType == null || w.Type == expectedType).ToList()
                    : stores.Where(w => StockMigrationSheet.Normal(w.Name) == StockMigrationSheet.Normal(line.WarehouseName)).ToList();
                if (named.Count == 0)
                    blockers.Add(string.IsNullOrWhiteSpace(line.WarehouseName)
                        ? $"No store given, and {department} has no {Describe(expectedType)} store to put it in."
                        : $"{department} has no store called \"{line.WarehouseName}\".");
                else if (string.IsNullOrWhiteSpace(line.ShelfName))
                {
                    (store, shelf) = named.Select(w => (w, s: FirstShelf(w))).FirstOrDefault(x => x.s != null);
                    if (shelf == null)
                        blockers.Add(named.Count == 1
                            ? $"No shelf given, and {named[0].Name} has no shelves. Add one or pick another store."
                            : $"No shelf given, and {department}'s stores have no shelves. Add one.");
                    else
                        warnings.Add($"No shelf given, so the first shelf of the first rack in {store.Name} ({shelf.Name}) is used.");
                }
                else
                {
                    var key = StockMigrationSheet.ShelfKey(line.ShelfName);
                    (store, shelf) = named.SelectMany(w => ShelvesOf(w).Select(s => (w, s)))
                        .FirstOrDefault(x => StockMigrationSheet.ShelfKey(x.s.Name) == key || StockMigrationSheet.ShelfKey(x.s.Code) == key);
                    if (shelf == null)
                        blockers.Add(named.Count == 1
                            ? $"There is no shelf {line.ShelfName} in {named[0].Name}. Add it to the store or pick another shelf."
                            : $"There is no shelf {line.ShelfName} in {department}'s stores. Add it or pick another shelf.");
                }
            }
            if (store != null && expectedType != null && store.Type != expectedType)
            {
                blockers.Add($"{material.Code} is a {(material.Kind == MaterialKind.Raw ? "raw" : "packaging")} material but {store.Name} is a {Describe(store.Type)} store.");
                shelf = null;
            }
            line.ResolvedShelfId = shelf?.Id;
            line.ResolvedWarehouseId = shelf == null ? null : store?.Id;

            // Unit and quantity
            var sheetUnit = string.IsNullOrWhiteSpace(line.UnitSymbol)
                ? null
                : units.FirstOrDefault(u => string.Equals(u.Symbol?.Trim(), line.UnitSymbol.Trim(), StringComparison.OrdinalIgnoreCase));
            UnitOfMeasure storage = null;
            if (material != null)
            {
                if (departmentUnits.TryGetValue(material.Id, out var departmentUnit))
                {
                    storage = departmentUnit;
                    if (sheetUnit != null && Family(sheetUnit.Symbol) != Family(storage.Symbol))
                        warnings.Add($"The sheet says {line.UnitSymbol}, but {department} keeps {material.Code} in {storage.Symbol}, so the quantity is read as {SheetUnitFor(migration, storage)}.");
                }
                else
                {
                    storage = sheetUnit;
                    warnings.Add($"{material.Code} is not set up for {department}.");
                    if (storage == null)
                        blockers.Add(string.IsNullOrWhiteSpace(line.UnitSymbol) ? "No unit." : $"\"{line.UnitSymbol}\" is not a unit in the system.");
                }
            }
            line.UoMId = storage?.Id;
            if (storage != null && line.QuantityInSheet is { } inSheet)
            {
                var factor = Family(storage.Symbol) switch
                {
                    "mass" => StockMigrationSheet.Factor(migration.MassUnit, storage.Symbol),
                    "volume" => StockMigrationSheet.Factor(migration.VolumeUnit, storage.Symbol),
                    _ => 1m,
                };
                line.Quantity = Tidy(inSheet * (factor ?? 1m));
            }

            // Batch and dates
            if (line.BatchNumberDamaged)
                blockers.Add($"Excel turned the batch number into a date (\"{line.SheetBatchNumber}\"). Type the real batch number.");
            else if (string.IsNullOrWhiteSpace(line.BatchNumber))
                blockers.Add("No batch number, and no GRN to use instead.");
            if (line.ExpiryDate is { } expiry && expiry < thisMonth)
                warnings.Add($"Expired ({expiry:MMM yyyy}).");
            if (line.ManufacturingDate is { } made && line.ExpiryDate is { } expires && made > expires)
                warnings.Add("The manufacturing date is after the expiry date.");
            if (line.ManufacturingDate is { } future && future > thisMonth)
                warnings.Add("The manufacturing date is in the future.");

            line.Blockers = Join(blockers);
            line.Warnings = Join(warnings);
            line.Status = line.Excluded ? StockMigrationLineStatus.Excluded
                : line.QuantityInSheet is null ? StockMigrationLineStatus.Skipped
                : blockers.Count > 0 ? StockMigrationLineStatus.Blocked
                : warnings.Count > 0 ? StockMigrationLineStatus.Warning
                : StockMigrationLineStatus.Ok;
        }

        // Rows against each other.
        var loading = lines
            .Where(l => l.Status is StockMigrationLineStatus.Ok or StockMigrationLineStatus.Warning && l.ResolvedMaterialId.HasValue)
            .ToList();
        foreach (var batch in loading.GroupBy(l => (l.ResolvedMaterialId, (l.BatchNumber ?? string.Empty).Trim().ToUpperInvariant())))
        {
            if (batch.Select(l => (l.ManufacturingDate, l.ExpiryDate)).Distinct().Count() > 1)
                foreach (var line in batch)
                {
                    line.Blockers = Join([.. Split(line.Blockers), $"Batch {line.BatchNumber} is also on {Rows(batch, line)} with different dates. Make them match."]);
                    line.Status = StockMigrationLineStatus.Blocked;
                }

            foreach (var onShelf in batch.GroupBy(l => l.ResolvedShelfId).Where(g => g.Count() > 1))
                foreach (var line in onShelf)
                {
                    line.Warnings = Join([.. Split(line.Warnings), $"The same batch is also on {Rows(onShelf, line)}; the quantities will be added together."]);
                    if (line.Status == StockMigrationLineStatus.Ok)
                        line.Status = StockMigrationLineStatus.Warning;
                }
        }
    }

    private static string Rows(IEnumerable<StockMigrationLine> group, StockMigrationLine self)
    {
        var others = group.Where(l => l != self).Select(l => l.Sheet == self.Sheet ? $"{l.RowNumber}" : $"{l.RowNumber} ({l.Sheet})").ToList();
        return $"row{(others.Count > 1 ? "s" : string.Empty)} {string.Join(", ", others)}";
    }

    private static readonly HashSet<string> GenericWords =
    [
        "carton", "cartons", "label", "labels", "leaflet", "leaflets", "shipper", "shippers", "syrup", "suspension",
        "susp", "tonic", "cap", "caps", "bottle", "bottles", "glass", "amber", "clear", "with", "and", "the", "for",
    ];

    /// <summary>A sheet name that shares little with the system's name for the code, suggesting the wrong code.</summary>
    private static bool LooksDifferent(string sheetName, string systemName)
    {
        if (StockMigrationSheet.Similarity(sheetName, systemName) >= 0.6)
            return false;
        var sheetWords = Words(sheetName);
        var systemWords = Words(systemName).Where(w => !GenericWords.Contains(w)).ToList();
        if (systemWords.Count == 0)
            return false;
        var found = systemWords.Count(w => sheetWords.Any(s => s == w || (w.Length >= 4 && s.StartsWith(w)) || (s.Length >= 4 && w.StartsWith(s))));
        return (double)found / systemWords.Count <= 0.5;
    }

    private static List<string> Words(string text) =>
        System.Text.RegularExpressions.Regex.Split((text ?? string.Empty).ToLowerInvariant(), "[^a-z0-9%]+")
            .Where(w => w.Length >= 2)
            .ToList();

    private static string SheetUnitFor(StockMigration migration, UnitOfMeasure storage) =>
        Family(storage.Symbol) switch
        {
            "mass" => migration.MassUnit,
            "volume" => migration.VolumeUnit,
            _ => storage.Symbol,
        };

    /// <summary>
    /// Whether a unit weighs, measures liquid or counts, judged by its symbol: the units' stored categories
    /// cannot be relied on (ml is filed as a weight on some servers).
    /// </summary>
    private static string Family(string symbol) =>
        StockMigrationSheet.IsMassUnit(symbol) ? "mass" : StockMigrationSheet.IsVolumeUnit(symbol) ? "volume" : "count";

    /// <summary>Drops trailing zeros a multiplication leaves (159.4 × 1,000,000 is 159400000, not 159400000.0).</summary>
    private static decimal Tidy(decimal value) => value / 1.0000000000000000000000000000m;

    private IQueryable<ShelfMaterialBatch> CurrentStock(List<Guid> storeIds, HashSet<MaterialKind> kinds) =>
        context.ShelfMaterialBatches.IgnoreQueryFilters().Where(s =>
            s.DeletedAt == null
            && s.Quantity != 0
            && storeIds.Contains(s.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.WarehouseId)
            && kinds.Contains(s.MaterialBatch.Material.Kind)
            && !s.MaterialBatch.Material.IsUnlimited
        );

    /// <summary>The kinds of material the migration would load, which decides whose stock it writes off.</summary>
    private async Task<HashSet<MaterialKind>> LoadedKinds(Guid id)
    {
        var materialIds = await context
            .StockMigrationLines.Where(l => l.StockMigrationId == id && l.ResolvedMaterialId != null
                && (l.Status == StockMigrationLineStatus.Ok || l.Status == StockMigrationLineStatus.Warning || l.Status == StockMigrationLineStatus.Blocked))
            .Select(l => l.ResolvedMaterialId.Value)
            .Distinct()
            .ToListAsync();
        return (await context.Materials.IgnoreQueryFilters().Where(m => materialIds.Contains(m.Id)).Select(m => m.Kind).Distinct().ToListAsync())
            .ToHashSet();
    }

    private async Task<List<Warehouse>> Stores(Guid departmentId) =>
        await context
            .Warehouses.IgnoreQueryFilters()
            .AsSplitQuery()
            .Include(w => w.Locations.Where(l => l.DeletedAt == null))
                .ThenInclude(l => l.Racks.Where(r => r.DeletedAt == null))
                    .ThenInclude(r => r.Shelves.Where(s => s.DeletedAt == null))
            .Where(w => w.DepartmentId == departmentId && w.DeletedAt == null
                && (w.Type == WarehouseType.RawMaterialStorage || w.Type == WarehouseType.PackagedStorage))
            .OrderBy(w => w.Name)
            .ToListAsync();

    private static IEnumerable<WarehouseLocationShelf> ShelvesOf(Warehouse store) =>
        store.Locations.SelectMany(l => l.Racks).SelectMany(r => r.Shelves);

    /// <summary>The store's first shelf on its first rack, by name, for rows the sheet gives no shelf.</summary>
    internal static WarehouseLocationShelf FirstShelf(Warehouse store) =>
        store.Locations.OrderBy(l => l.Name, StringComparer.OrdinalIgnoreCase)
            .SelectMany(l => l.Racks.OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase))
            .SelectMany(r => r.Shelves.OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase))
            .FirstOrDefault();

    private async Task<Result<StockMigration>> Draft(Guid id)
    {
        var migration = await context.StockMigrations.Include(m => m.Lines).FirstOrDefaultAsync(m => m.Id == id);
        if (migration == null)
            return NotFound;
        if (migration.Status != StockMigrationStatus.Draft)
            return Error.Conflict("StockMigration.NotDraft", "This migration has already been approved or discarded.");
        return migration;
    }

    private static StockMigrationComparisonDto Row(Dictionary<Guid, StockMigrationComparisonDto> rows, Material material, string unit)
    {
        if (!rows.TryGetValue(material.Id, out var row))
            rows[material.Id] = row = new StockMigrationComparisonDto
            {
                MaterialId = material.Id,
                Code = material.Code,
                Name = material.Name,
                Kind = material.Kind,
                Unit = unit,
            };
        row.Unit ??= unit;
        return row;
    }

    private static WarehouseType StoreFor(MaterialKind kind) =>
        kind == MaterialKind.Raw ? WarehouseType.RawMaterialStorage : WarehouseType.PackagedStorage;

    private static MaterialKind KindOf(WarehouseType type) =>
        type == WarehouseType.RawMaterialStorage ? MaterialKind.Raw : MaterialKind.Package;

    private static string Describe(WarehouseType? type) =>
        type switch
        {
            WarehouseType.RawMaterialStorage => "raw material",
            WarehouseType.PackagedStorage => "packaging",
            WarehouseType.FinishedGoodsStorage => "finished goods",
            WarehouseType.Production => "production",
            _ => "storage",
        };

    private static string Unit(string unit, string[] allowed) =>
        allowed.FirstOrDefault(a => string.Equals(a, unit?.Trim(), StringComparison.OrdinalIgnoreCase));

    private static DateTime? Utc(DateTime? date) =>
        date is { } d ? new DateTime(d.Year, d.Month, 1, 0, 0, 0, DateTimeKind.Utc) : null;

    private static List<string> Split(string text) =>
        string.IsNullOrEmpty(text) ? [] : text.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList();

    private static string Join(IEnumerable<string> items) => Clip(string.Join('\n', items), 4000);

    private static string Clip(string text, int length) =>
        text is null ? null : text.Length <= length ? text : text[..length];

    private static Error NotFound => Error.NotFound("StockMigration.NotFound", "The stock migration was not found.");
}
