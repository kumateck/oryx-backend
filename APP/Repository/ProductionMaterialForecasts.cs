using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Reports;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

/// <summary>
/// Read-only material forecast for a department's monthly production plan. Requirements come from the same
/// source as production start (<see cref="ProductionMaterialSources.Load"/>, or the applied configuration of a
/// started batch); free stock uses the allocator's rules: approved, not expired or past retest
/// (<see cref="MaterialValidity"/>), on the department's storage shelves, minus every unissued reservation.
/// </summary>
internal static class ProductionMaterialForecasts
{
    private sealed record BatchRow(Guid Id, Guid ScheduleId, string ScheduleCode, DateTime ScheduleStart, DateTime ScheduleEnd,
        ProductionStatus ScheduleStatus, Guid ProductId, string ProductCode, string ProductName, string BatchNumber,
        BatchSize BatchSize, decimal Quantity, Guid? PackingId, string PackingName, bool Started)
    {
        internal string Key => $"{ProductId:N}:{BatchSize}:{PackingId?.ToString("N") ?? "none"}";
    }

    /// <summary>One batch's need for one material, in the material's department stock unit.</summary>
    private sealed class Line(BatchRow batch, MaterialKind kind, ProductionMaterialRequirement requirement)
    {
        internal BatchRow Batch { get; } = batch;
        internal MaterialKind Kind { get; } = kind;
        internal ProductionMaterialRequirement Requirement { get; } = requirement;
        internal decimal Reserved { get; set; }
        internal decimal Allocated { get; set; }
        internal bool Covered => Reserved + Allocated >= Requirement.Quantity;
    }

    internal static async Task<Result<ProductionMaterialForecastDto>> Build(ApplicationDbContext context, Guid departmentId,
        int year, int month, CancellationToken cancellationToken = default)
    {
        if (year is < 2000 or > 2100 || month is < 1 or > 12)
            return Error.Validation("Forecast.Month", "Choose a valid month.");
        var department = await context.Departments.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
            .Where(item => item.Id == departmentId && item.DeletedAt == null)
            .Select(item => new { item.Id, item.Name }).SingleOrDefaultAsync(cancellationToken);
        if (department is null) return Error.NotFound("Forecast.Department", "Your department was not found.");

        var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);
        // A schedule belongs to the month when its run overlaps it; a missing end counts as the start day.
        var batches = await context.ProductionScheduleProducts.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
            .Where(item => !item.Cancelled && item.ProductionSchedule.DeletedAt == null && item.Product.DeletedAt == null
                && (item.ProductionSchedule.DepartmentId ?? item.Product.DepartmentId) == departmentId
                && item.ProductionSchedule.ScheduledStartTime < end
                && (item.ProductionSchedule.ScheduledEndTime >= start || item.ProductionSchedule.ScheduledStartTime >= start))
            .Select(item => new BatchRow(item.Id, item.ProductionScheduleId, item.ProductionSchedule.Code,
                item.ProductionSchedule.ScheduledStartTime, item.ProductionSchedule.ScheduledEndTime, item.ProductionSchedule.Status,
                item.ProductId, item.Product.Code, item.Product.Name, item.BatchNumber, item.BatchSize, item.Quantity,
                item.ProductPackingId, item.ProductPacking == null ? null : item.ProductPacking.Name,
                context.ProductionActivities.IgnoreQueryFilters().Any(activity => activity.ProductionScheduleProductId == item.Id && activity.DeletedAt == null)))
            .ToListAsync(cancellationToken);
        batches = batches.OrderBy(item => item.ScheduleStart).ThenBy(item => item.ScheduleCode, StringComparer.Ordinal)
            .ThenBy(item => item.BatchNumber, StringComparer.Ordinal).ThenBy(item => item.Id).ToList();
        var batchIds = batches.Select(item => item.Id).ToList();

        var report = new ProductionMaterialForecastDto
        {
            Year = year, Month = month, PeriodStart = start, PeriodEnd = end, DepartmentId = department.Id,
            DepartmentName = department.Name, GeneratedAt = DateTime.UtcNow, TotalBatches = batches.Count
        };

        // Started batches keep the requirements applied when their stock was reserved.
        var configurations = await context.ProductionMaterialConfigurations.IgnoreQueryFilters().AsNoTracking()
            .Where(item => batchIds.Contains(item.ProductionScheduleProductId) && item.DeletedAt == null)
            .ToListAsync(cancellationToken);
        var lines = new List<Line>();
        var batchWarnings = batches.ToDictionary(item => item.Id, _ => new List<string>());
        var unavailable = new HashSet<(Guid, MaterialKind)>();
        foreach (var batch in batches)
        {
            foreach (var kind in new[] { MaterialKind.Raw, MaterialKind.Package })
            {
                var label = kind == MaterialKind.Raw ? "Raw materials" : "Packaging";
                var applied = configurations.FirstOrDefault(item => item.ProductionScheduleProductId == batch.Id && item.Kind == kind);
                List<ProductionMaterialRequirement> requirements;
                if (applied is not null) requirements = ProductionMaterialSources.Requirements(applied.RequirementsJson);
                else
                {
                    var loaded = await ProductionMaterialSources.Load(context, batch.Id, kind);
                    if (loaded.IsFailure)
                    {
                        unavailable.Add((batch.Id, kind));
                        batchWarnings[batch.Id].Add($"{label}: {loaded.Error.Description}");
                        continue;
                    }
                    requirements = loaded.Value.Requirements;
                    if (loaded.Value.Blockers.Count > 0)
                    {
                        unavailable.Add((batch.Id, kind));
                        batchWarnings[batch.Id].AddRange(loaded.Value.Blockers.Select(blocker => $"{label}: {blocker}"));
                    }
                }
                lines.AddRange(requirements.Select(requirement => new Line(batch, kind, requirement)));
            }
        }

        // Stock already reserved for, or issued to, a planned batch covers that batch first.
        var own = await context.MaterialBatchReservedQuantities.IgnoreQueryFilters().AsNoTracking()
            .Where(item => batchIds.Contains(item.ProductionScheduleProductId) && item.ReleasedAt == null && item.ProductionUnstartRepairId == null)
            .Select(item => new { BatchId = item.ProductionScheduleProductId, item.MaterialBatch.MaterialId, item.Quantity, Unit = item.UoM.Symbol })
            .ToListAsync(cancellationToken);
        foreach (var line in lines)
        {
            var held = own.Where(item => item.BatchId == line.Batch.Id && item.MaterialId == line.Requirement.MaterialId)
                .Select(item => UomConverter.Convert(item.Quantity, item.Unit, line.Requirement.Unit)).ToList();
            if (held.Any(value => !value.HasValue))
                batchWarnings[line.Batch.Id].Add($"{line.Requirement.Name}: reserved stock units cannot be converted to {line.Requirement.Unit}.");
            line.Reserved = Math.Min(line.Requirement.Quantity, held.Sum(value => value.GetValueOrDefault()));
        }

        var free = new Dictionary<Guid, decimal>();
        foreach (var kind in new[] { MaterialKind.Raw, MaterialKind.Package })
        {
            var ofKind = lines.Where(item => item.Kind == kind).ToList();
            if (ofKind.Count == 0) continue;
            var type = kind == MaterialKind.Raw ? WarehouseType.RawMaterialStorage : WarehouseType.PackagedStorage;
            var storages = await context.Warehouses.IgnoreQueryFilters().AsNoTracking()
                .Where(item => item.DepartmentId == departmentId && item.DeletedAt == null && item.Type == type)
                .Select(item => item.Id).ToListAsync(cancellationToken);
            if (storages.Count != 1)
            {
                report.Warnings.Add($"Exactly one {(kind == MaterialKind.Raw ? "raw material" : "packaging")} storage warehouse must be configured for this department; free stock is shown as zero.");
                continue;
            }
            var units = ofKind.GroupBy(item => item.Requirement.MaterialId)
                .ToDictionary(group => group.Key, group => group.First().Requirement);
            var stock = await FreeStock(context, storages[0], units, report.Warnings, cancellationToken);
            foreach (var pair in stock) free[pair.Key] = pair.Value;
            // Share free stock out in schedule order, a batch at a time.
            foreach (var group in ofKind.GroupBy(item => item.Requirement.MaterialId))
            {
                var remaining = free.GetValueOrDefault(group.Key);
                foreach (var line in group)
                {
                    var give = Math.Min(Math.Max(0, line.Requirement.Quantity - line.Reserved), remaining);
                    line.Allocated = give;
                    remaining -= give;
                }
            }
        }

        var materialIds = lines.Select(item => item.Requirement.MaterialId).Distinct().ToList();
        var materials = await context.Materials.IgnoreQueryFilters().AsNoTracking().Where(item => materialIds.Contains(item.Id))
            .Select(item => new { item.Id, item.Code, item.Name }).ToDictionaryAsync(item => item.Id, cancellationToken);

        bool Ready(BatchRow batch, MaterialKind kind) => !unavailable.Contains((batch.Id, kind))
            && lines.Where(item => item.Batch.Id == batch.Id && item.Kind == kind).All(item => item.Covered);

        var batchDtos = batches.ToDictionary(batch => batch.Id, batch => new ProductionMaterialForecastBatchDto
        {
            Id = batch.Id, ScheduleId = batch.ScheduleId, ProductKey = batch.Key, ProductId = batch.ProductId,
            ProductCode = batch.ProductCode, ProductName = batch.ProductName, BatchNumber = batch.BatchNumber,
            BatchSize = batch.BatchSize, Quantity = batch.Quantity, PackingStyleId = batch.PackingId, PackingStyle = batch.PackingName,
            Started = batch.Started,
            RawReserved = configurations.Any(item => item.ProductionScheduleProductId == batch.Id && item.Kind == MaterialKind.Raw),
            PackagingReserved = configurations.Any(item => item.ProductionScheduleProductId == batch.Id && item.Kind == MaterialKind.Package),
            RawReady = Ready(batch, MaterialKind.Raw), PackagingReady = Ready(batch, MaterialKind.Package),
            Warnings = batchWarnings[batch.Id].Distinct().ToList()
        });
        report.RawReadyBatches = batchDtos.Values.Count(item => item.RawReady);
        report.PackagingReadyBatches = batchDtos.Values.Count(item => item.PackagingReady);
        report.Schedules = batches.GroupBy(item => item.ScheduleId).Select(group => new ProductionMaterialForecastScheduleDto
        {
            Id = group.Key, Code = group.First().ScheduleCode, ScheduledStartTime = group.First().ScheduleStart,
            ScheduledEndTime = group.First().ScheduleEnd, Status = group.First().ScheduleStatus,
            Batches = group.Select(item => batchDtos[item.Id]).ToList()
        }).ToList();

        report.Products = batches.GroupBy(item => item.Key).Select(group =>
        {
            var first = group.First();
            var ids = group.Select(item => item.Id).ToHashSet();
            List<ProductionMaterialForecastMaterialDto> Materials(MaterialKind kind) => lines
                .Where(item => item.Kind == kind && ids.Contains(item.Batch.Id))
                .GroupBy(item => item.Requirement.MaterialId)
                .Select(rows =>
                {
                    var sample = rows.First().Requirement;
                    var reserved = rows.Sum(item => item.Reserved);
                    var satisfied = reserved + rows.Sum(item => item.Allocated);
                    var total = rows.Sum(item => item.Requirement.Quantity);
                    var material = materials.GetValueOrDefault(rows.Key);
                    return new ProductionMaterialForecastMaterialDto
                    {
                        MaterialId = rows.Key, MaterialCode = material?.Code, MaterialName = material?.Name ?? sample.Name,
                        Kind = kind, UnitId = sample.UnitId, Unit = sample.Unit, PerBatchQuantity = sample.Quantity,
                        PerBatchQuantityVaries = rows.Any(item => item.Requirement.Quantity != sample.Quantity),
                        BatchCount = rows.Count(), TotalQuantity = total, ReservedQuantity = reserved,
                        CanSatisfyQuantity = satisfied, ShortfallQuantity = Math.Max(0, total - satisfied),
                        BatchesSatisfiable = rows.Count(item => item.Covered), FreeInWarehouse = free.GetValueOrDefault(rows.Key),
                        SharedWithProducts = lines.Where(item => item.Requirement.MaterialId == rows.Key && !ids.Contains(item.Batch.Id))
                            .Select(item => item.Batch.Key).Distinct().Count()
                    };
                }).ToList();
            return new ProductionMaterialForecastProductDto
            {
                Key = group.Key, ProductId = first.ProductId, ProductCode = first.ProductCode, ProductName = first.ProductName,
                BatchSize = first.BatchSize, PackingStyleId = first.PackingId, PackingStyle = first.PackingName,
                BatchCount = group.Count(), BatchIds = group.Select(item => item.Id).ToList(),
                RawBatchesSatisfiable = group.Count(batch => batchDtos[batch.Id].RawReady),
                PackagingBatchesSatisfiable = group.Count(batch => batchDtos[batch.Id].PackagingReady),
                BatchesSatisfiable = group.Count(batch => batchDtos[batch.Id].RawReady && batchDtos[batch.Id].PackagingReady),
                RawMaterials = Materials(MaterialKind.Raw), PackagingMaterials = Materials(MaterialKind.Package),
                Warnings = group.SelectMany(batch => batchDtos[batch.Id].Warnings).Distinct().ToList()
            };
        }).ToList();
        report.Warnings = report.Warnings.Distinct().ToList();
        return report;
    }

    /// <summary>
    /// Usable stock per material on the storage warehouse's shelves, less unissued reservations of any batch on the
    /// same shelf and lot, in the requirement unit. Same rules as <see cref="ProductionMaterialAllocator.Plan"/>.
    /// </summary>
    private static async Task<Dictionary<Guid, decimal>> FreeStock(ApplicationDbContext context, Guid storageId,
        Dictionary<Guid, ProductionMaterialRequirement> requirements, List<string> warnings, CancellationToken cancellationToken)
    {
        var ids = requirements.Keys.ToList();
        var shelves = await context.ShelfMaterialBatches.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
            .Include(item => item.MaterialBatch).ThenInclude(item => item.UoM)
            .Where(item => ids.Contains(item.MaterialBatch.MaterialId) && item.DeletedAt == null && item.MaterialBatch.DeletedAt == null
                && item.Quantity > 0 && item.MaterialBatch.Status == BatchStatus.Available
                && item.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.WarehouseId == storageId
                && item.WarehouseLocationShelf.DeletedAt == null && item.WarehouseLocationShelf.WarehouseLocationRack.DeletedAt == null
                && item.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.DeletedAt == null)
            .ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;
        shelves = shelves.Where(item => !MaterialValidity.IsExpired(item.MaterialBatch, now)).ToList();
        var shelfIds = shelves.Select(item => item.WarehouseLocationShelfId).Distinct().ToList();
        var holds = await ProductionMaterialAllocator.UnissuedReservations(context).IgnoreAutoIncludes().AsNoTracking()
            .Where(item => item.WarehouseLocationShelfId.HasValue && shelfIds.Contains(item.WarehouseLocationShelfId.Value))
            .Select(item => new { item.MaterialBatchId, item.WarehouseLocationShelfId, item.Quantity, Unit = item.UoM.Symbol })
            .ToListAsync(cancellationToken);
        var result = ids.ToDictionary(id => id, _ => 0m);
        foreach (var shelf in shelves)
        {
            var requirement = requirements[shelf.MaterialBatch.MaterialId];
            if (shelf.UoMId != shelf.MaterialBatch.UoMId || shelf.MaterialBatch.UoMId != requirement.UnitId)
            {
                warnings.Add($"{requirement.Name}: shelf, batch and department stock units must agree; that stock is not counted.");
                continue;
            }
            var available = UomConverter.Convert(shelf.Quantity, shelf.MaterialBatch.UoM?.Symbol, requirement.Unit);
            var held = holds.Where(item => item.MaterialBatchId == shelf.MaterialBatchId && item.WarehouseLocationShelfId == shelf.WarehouseLocationShelfId)
                .Select(item => UomConverter.Convert(item.Quantity, item.Unit, requirement.Unit)).ToList();
            if (!available.HasValue || held.Any(value => !value.HasValue))
            {
                warnings.Add($"{requirement.Name}: stock or reservation units cannot be converted safely; that stock is not counted.");
                continue;
            }
            result[shelf.MaterialBatch.MaterialId] += Math.Max(0, available.Value - held.Sum(value => value.GetValueOrDefault()));
        }
        return result;
    }
}
