using AutoMapper;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Requisitions;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

internal static class ProductionMaterialQueries
{
    private sealed record Row(ProductionMaterialRequirement Requirement, Material Material, Material Parent,
        Material Direct, MaterialDepartment Department, UnitOfMeasure Unit, UnitOfMeasure BaseUnit, decimal OnHand,
        decimal Reserved, decimal ReservedAll, decimal Consumed, decimal Extra, MaterialRequisitionStatus Status);

    internal static async Task<Result<List<ProductionScheduleProcurementDto>>> Raw(ApplicationDbContext context,
        IMapper mapper, ProductionMaterialConfiguration configuration, MaterialRequisitionStatus? status)
    {
        var result = await Read(context, configuration);
        if (result.IsFailure) return result.Error;
        return result.Value.Where(row => !status.HasValue || row.Status == status).Select(row => new ProductionScheduleProcurementDto
        {
            Material = mapper.Map<MaterialDto>(row.Material), BaseUoM = mapper.Map<UnitOfMeasureDto>(row.BaseUnit),
            BaseQuantity = row.Requirement.BaseQuantity, PrescribedQuantity = row.Requirement.PrescribedQuantity,
            QuantityNeeded = row.Requirement.Quantity, QuantityOnHand = row.OnHand, Status = row.Status,
            StorageWarehouseId = configuration.StorageWarehouseId, ProductionWarehouseId = configuration.ProductionWarehouseId,
            MaterialDepartment = Department(mapper, row), FrozenQuantity = row.Reserved, TotalFrozenQuantity = row.ReservedAll,
            ConsumedQuantity = row.Consumed, ExtraQuantity = row.Extra, IsSubstitute = row.Parent is not null,
            ParentMaterial = mapper.Map<MaterialDto>(row.Parent)
        }).ToList();
    }

    internal static async Task<Result<List<ProductionScheduleProcurementPackageDto>>> Packaging(ApplicationDbContext context,
        IMapper mapper, ProductionMaterialConfiguration configuration, MaterialRequisitionStatus? status)
    {
        var result = await Read(context, configuration);
        if (result.IsFailure) return result.Error;
        return result.Value.Where(row => !status.HasValue || row.Status == status).Select(row => new ProductionScheduleProcurementPackageDto
        {
            Material = mapper.Map<MaterialDto>(row.Material), DirectLinkMaterial = mapper.Map<MaterialDto>(row.Direct),
            BaseQuantity = row.Requirement.BaseQuantity, PrescribedQuantity = row.Requirement.PrescribedQuantity,
            QuantityNeeded = row.Requirement.Quantity, QuantityOnHand = row.OnHand, Status = row.Status,
            UnitCapacity = row.Requirement.UnitCapacity, PackingExcessMargin = row.Requirement.Allowance,
            StorageWarehouseId = configuration.StorageWarehouseId, ProductionWarehouseId = configuration.ProductionWarehouseId,
            MaterialDepartment = Department(mapper, row), FrozenQuantity = row.Reserved, TotalFrozenQuantity = row.ReservedAll,
            ConsumedQuantity = row.Consumed, ExtraQuantity = row.Extra, IsSubstitute = row.Parent is not null,
            ParentMaterial = mapper.Map<MaterialDto>(row.Parent)
        }).ToList();
    }

    private static MaterialDepartmentDetails Department(IMapper mapper, Row row) => new()
    {
        Department = mapper.Map<CollectionItemDto>(row.Department?.Department), UoM = mapper.Map<UnitOfMeasureDto>(row.Unit),
        Density = row.Department?.Density ?? 0, ReOrderLevel = row.Department?.ReOrderLevel ?? 0,
        MinimumStockLevel = row.Department?.MinimumStockLevel ?? 0, MaximumStockLevel = row.Department?.MaximumStockLevel ?? 0
    };

    private static async Task<Result<List<Row>>> Read(ApplicationDbContext context, ProductionMaterialConfiguration configuration)
    {
        var requirements = ProductionMaterialSources.Requirements(configuration.RequirementsJson);
        var ids = requirements.SelectMany(item => new[] { item.MaterialId, item.SourceMaterialId, item.DirectLinkMaterialId.GetValueOrDefault() }).Distinct().ToList();
        var materials = await context.Materials.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking().AsSplitQuery().Include(item => item.MaterialCategory)
            .Include(item => item.Batches.Where(batch => batch.DeletedAt == null))
                .ThenInclude(batch => batch.ReservedQuantities.Where(item => item.DeletedAt == null && item.ReleasedAt == null))
            .Where(item => ids.Contains(item.Id)).ToDictionaryAsync(item => item.Id);
        var units = await context.UnitOfMeasures.IgnoreQueryFilters().AsNoTracking().ToDictionaryAsync(item => item.Id);
        var departments = await context.MaterialDepartments.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking().Include(item => item.Department)
            .Where(item => ids.Contains(item.MaterialId) && item.DepartmentId == configuration.DepartmentId && item.DeletedAt == null).ToListAsync();
        var shelves = await context.ShelfMaterialBatches.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking().Include(item => item.MaterialBatch)
            .Where(item => ids.Contains(item.MaterialBatch.MaterialId) && item.DeletedAt == null && item.MaterialBatch.DeletedAt == null
                && item.MaterialBatch.Status == BatchStatus.Available
                && (item.MaterialBatch.ExpiryDate == null || item.MaterialBatch.ExpiryDate == DateTime.MinValue || item.MaterialBatch.ExpiryDate >= DateTime.UtcNow)
                && item.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.WarehouseId == configuration.StorageWarehouseId).ToListAsync();
        var reservations = await context.MaterialBatchReservedQuantities.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking().Include(item => item.MaterialBatch)
            .Where(item => ids.Contains(item.MaterialBatch.MaterialId) && item.WarehouseId == configuration.ProductionWarehouseId && item.ReleasedAt == null).ToListAsync();
        var items = await context.RequisitionItems.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking().Include(item => item.Requisition)
            .Where(item => item.Requisition.ProductionScheduleProductId == configuration.ProductionScheduleProductId
                && item.Requisition.RequisitionType == RequisitionType.Stock && item.DeletedAt == null && item.Requisition.DeletedAt == null).ToListAsync();
        var extras = await context.ProductionExtraPackings.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
            .Where(item => item.ProductionScheduleProductId == configuration.ProductionScheduleProductId && item.DeletedAt == null
                && item.Status == ProductionExtraPackingStatus.Approved).ToListAsync();
        var result = new List<Row>();
        foreach (var requirement in requirements)
        {
            if (!materials.TryGetValue(requirement.MaterialId, out var material) || !units.TryGetValue(requirement.UnitId, out var unit))
                return Error.Validation("Production.Configuration", "The applied material or unit cannot be read. Reconcile this batch's configuration.");
            if (!string.Equals(unit.Symbol?.Trim(), requirement.Unit.Trim(), StringComparison.OrdinalIgnoreCase))
                return Error.Validation("Production.Units", "The applied stock unit's definition changed. Reconcile the units before continuing.");
            decimal? Sum(IEnumerable<(decimal Quantity, Guid Unit)> values)
            {
                var converted = values.Select(value => UomConverter.Convert(value.Quantity, units.GetValueOrDefault(value.Unit)?.Symbol, requirement.Unit)).ToList();
                return converted.Any(value => !value.HasValue) ? null : converted.Sum(value => value.GetValueOrDefault());
            }
            var matching = reservations.Where(item => item.MaterialBatch.MaterialId == requirement.MaterialId).ToList();
            var own = matching.Where(item => item.ProductionScheduleProductId == configuration.ProductionScheduleProductId).ToList();
            var onHand = Sum(shelves.Where(item => item.MaterialBatch.MaterialId == requirement.MaterialId).Select(item => (item.Quantity, item.UoMId.GetValueOrDefault())));
            var reserved = Sum(own.Where(item => item.DeletedAt == null).Select(item => (item.Quantity, item.UoMId)));
            var all = Sum(matching.Where(item => item.DeletedAt == null).Select(item => (item.Quantity, item.UoMId)));
            var consumed = Sum(own.Where(item => item.DeletedAt.HasValue).Select(item => (item.Quantity, item.UoMId)));
            var extra = Sum(extras.Where(item => item.MaterialId == requirement.MaterialId).Select(item => (item.Quantity, item.UoMId)));
            if (onHand is null || reserved is null || all is null || consumed is null || extra is null)
                return Error.Validation("Production.Units", "Material stock units no longer agree with the applied batch configuration.");
            var requested = items.Where(item => item.MaterialId == requirement.MaterialId).ToList();
            var state = consumed > 0 ? MaterialRequisitionStatus.Supplied
                : requested.Any(item => item.Status == RequestStatus.Completed || item.Requisition.Status == RequestStatus.Completed) ? MaterialRequisitionStatus.Issued
                : requested.Count > 0 ? MaterialRequisitionStatus.StockRequisition
                : reserved + Math.Max(0, onHand.Value - all.Value) >= requirement.Quantity ? MaterialRequisitionStatus.InHouse : MaterialRequisitionStatus.None;
            result.Add(new Row(requirement, material, requirement.SourceMaterialId != requirement.MaterialId ? materials.GetValueOrDefault(requirement.SourceMaterialId) : null,
                materials.GetValueOrDefault(requirement.DirectLinkMaterialId.GetValueOrDefault()), departments.FirstOrDefault(item => item.MaterialId == requirement.MaterialId),
                unit, units.GetValueOrDefault(requirement.BaseUnitId) ?? unit, onHand.Value, reserved.Value, all.Value, consumed.Value, extra.Value, state));
        }
        return result;
    }
}
