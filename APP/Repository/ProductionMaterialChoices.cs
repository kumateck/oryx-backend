using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Products.Production;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

namespace APP.Repository;

/// <summary>What one material option of a BOM line would draw from the storage warehouse.</summary>
internal sealed record ProductionMaterialOptionStock(ProductionMaterialOption Option, decimal Available, bool Covers,
    DateTime? EarliestExpiry, List<ProductionMaterialAllocation> Allocations, List<string> Blockers, List<string> ShortageDetails);

/// <summary>
/// A BOM line is issued as exactly one of its options: the main material or a substitute, each with its own quantity.
/// These rules evaluate the options against usable, unreserved stock in the batch's storage warehouse.
/// </summary>
internal static class ProductionMaterialChoices
{
    // Planning this much shows everything usable without capping it at the requirement.
    private const decimal Everything = 1_000_000_000_000m;

    /// <summary>For lines short of their current option: the first other option (BOM order) that is fully in stock.</summary>
    internal static async Task<Dictionary<Guid, Guid>> FirstCoveredOptions(ApplicationDbContext context, ProductionMaterialSource source,
        ProductionMaterialAllocationPlan plan)
    {
        var chosen = new Dictionary<Guid, Guid>();
        foreach (var requirement in source.Requirements.Where(item => item.Options is { Count: > 1 }
                     && plan.Shortages.GetValueOrDefault(item.MaterialId) > 0))
        {
            foreach (var option in requirement.Options.Where(item => item.MaterialId != requirement.MaterialId))
            {
                if (source.Requirements.Any(other => other.MaterialId == option.MaterialId)) continue;
                var stock = await Evaluate(context, source, requirement, option, []);
                if (!stock.Covers) continue;
                chosen[requirement.SourceMaterialId] = option.MaterialId;
                break;
            }
        }
        return chosen;
    }

    /// <summary>
    /// Plans <paramref name="option"/> alone for the line. <paramref name="own"/> are this batch's current holds of that
    /// material, which count as available to it.
    /// </summary>
    internal static async Task<ProductionMaterialOptionStock> Evaluate(ApplicationDbContext context, ProductionMaterialSource source,
        ProductionMaterialRequirement requirement, ProductionMaterialOption option, List<MaterialBatchReservedQuantity> own)
    {
        var target = ProductionMaterialSources.Requirement(requirement, option);
        var plan = await ProductionMaterialAllocator.Plan(context, source with { Requirements = [target] }, own);
        var everything = await ProductionMaterialAllocator.Plan(context, source with { Requirements = [target with { Quantity = Everything }] }, own);
        var available = Everything - everything.Shortages.GetValueOrDefault(option.MaterialId);
        var batchIds = everything.Allocations.Select(item => item.BatchId).Distinct().ToList();
        var batches = await context.MaterialBatches.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
            .Where(batch => batchIds.Contains(batch.Id)).ToListAsync();
        var earliest = batches.Select(batch => MaterialValidity.EffectiveDate(batch)).Where(date => date.HasValue && date != DateTime.MinValue).Min();
        var covers = plan.Blockers.Count == 0 && plan.Shortages.GetValueOrDefault(option.MaterialId) <= 0;
        return new ProductionMaterialOptionStock(option, available, covers, earliest, plan.Allocations, plan.Blockers, plan.ShortageDetails);
    }
}
