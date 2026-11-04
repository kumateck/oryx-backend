using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Requisitions.Request;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

internal static class ProductionMaterialRequisitionRules
{
    internal static async Task<Result> ValidateProcurement(ApplicationDbContext context, Guid batchId, List<CreateRequisitionItemRequest> items)
    {
        var configurations = await context.ProductionMaterialConfigurations.AsNoTracking().Where(item => item.ProductionScheduleProductId == batchId).ToListAsync();
        if (configurations.Count == 0) return Result.Success();
        if (items.GroupBy(item => item.MaterialId).Any(group => group.Count() != 1))
            return Error.Validation("Procurement.Materials", "Select each material once when requesting supply for an applied batch configuration.");
        var materials = await context.Materials.Where(material => items.Select(item => item.MaterialId).Contains(material.Id))
            .Select(material => new { material.Id, material.Kind }).ToListAsync();
        var units = await context.UnitOfMeasures.AsNoTracking().ToDictionaryAsync(item => item.Id);
        var extras = await context.ProductionExtraPackings.AsNoTracking().Where(item => item.ProductionScheduleProductId == batchId
            && item.Approved && item.Status == ProductionExtraPackingStatus.InProgress).ToListAsync();
        foreach (var item in items)
        {
            var material = materials.SingleOrDefault(material => material.Id == item.MaterialId);
            if (material is null) return Error.Validation("Procurement.Material", "A requested material no longer exists.");
            var configuration = configurations.SingleOrDefault(configuration => configuration.Kind == material?.Kind);
            if (configuration is null) continue;
            var requirement = ProductionMaterialSources.Requirements(configuration.RequirementsJson).SingleOrDefault(line => line.MaterialId == item.MaterialId);
            var approvedExtras = extras.Where(extra => extra.MaterialId == item.MaterialId).ToList();
            var unitId = requirement?.UnitId ?? approvedExtras.FirstOrDefault()?.UoMId;
            var symbol = unitId.HasValue ? units.GetValueOrDefault(unitId.Value)?.Symbol : null;
            var quantity = UomConverter.Convert(item.Quantity, units.GetValueOrDefault(item.UomId.GetValueOrDefault())?.Symbol, symbol);
            var additional = approvedExtras.Select(extra => UomConverter.Convert(extra.Quantity, units.GetValueOrDefault(extra.UoMId)?.Symbol, symbol)).ToList();
            if (quantity is null or <= 0 || additional.Any(value => !value.HasValue)
                || quantity > (requirement?.Quantity ?? 0) + additional.Sum(value => value.GetValueOrDefault()))
                return Error.Conflict("Procurement.StaleConfiguration", "The material, quantity or unit no longer matches this batch's applied requirements and approved extra material. Reload the batch before requesting supply.");
        }
        return Result.Success();
    }

    internal static async Task<Result> Validate(ApplicationDbContext context, Guid batchId, List<CreateRequisitionItemRequest> items)
    {
        var units = await ProductionMaterialUnitRules.ValidateStock(context, batchId, items);
        if (units.IsFailure) return units;
        if (items.Count == 0 || items.GroupBy(item => item.MaterialId).Any(group => group.Count() != 1))
            return Error.Validation("Requisition.Materials", "Choose each required material exactly once.");
        var materials = await context.Materials.Where(item => items.Select(line => line.MaterialId).Contains(item.Id))
            .Select(item => new { item.Id, item.Kind }).ToListAsync();
        if (materials.Count != items.Count) return Error.Validation("Requisition.Materials", "A material no longer exists.");
        foreach (var kind in materials.Select(item => item.Kind).Distinct())
        {
            var configuration = await context.ProductionMaterialConfigurations.AsNoTracking()
                .SingleOrDefaultAsync(item => item.ProductionScheduleProductId == batchId && item.Kind == kind);
            if (configuration is null) continue;
            var requirements = ProductionMaterialSources.Requirements(configuration.RequirementsJson);
            var selected = items.Where(item => materials.Any(material => material.Id == item.MaterialId && material.Kind == kind)).ToList();
            if (selected.Count != requirements.Count || requirements.Any(requirement => !selected.Any(item =>
                item.MaterialId == requirement.MaterialId && item.UomId == requirement.UnitId && item.Quantity == requirement.Quantity)))
                return Error.Conflict("Requisition.StaleConfiguration", "The batch requirements changed. Reload the batch before requesting stock.");
        }
        return Result.Success();
    }
}
