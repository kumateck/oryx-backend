using DOMAIN.Entities.Forms;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

namespace APP.Services.Forms;

/// <summary>Keep references saved on reusable sections within their specification's scope.</summary>
internal static class SpecificationTestReferences
{
    public static async Task<Dictionary<Guid, string>> Values(ApplicationDbContext db, Response response)
    {
        var sections = await db.FormSections.AsNoTracking().Where(section => section.FormId == response.FormId)
            .Select(section => new
            {
                section.Id, section.Value, section.ProductSpecificationId, section.MaterialSpecificationId,
                ProductNumber = section.ProductSpecification.SpecificationNumber,
                MaterialNumber = section.MaterialSpecification.SpecificationNumber
            }).ToListAsync();
        var productIds = new HashSet<Guid>();
        var materialIds = new HashSet<Guid>();
        var productNumbers = new HashSet<string>();
        var materialNumbers = new HashSet<string>();
        if (response.BatchManufacturingRecordId.HasValue)
        {
            var atr = await db.AnalyticalTestRequests.AsNoTracking()
                .Where(item => item.BatchManufacturingRecordId == response.BatchManufacturingRecordId
                    && item.ProductionActivityStepId == response.ProductionActivityStepId)
                .OrderByDescending(item => item.CreatedAt)
                .Select(item => new { item.ProductionScheduleProduct.ProductId, item.Stage }).FirstOrDefaultAsync();
            if (atr is not null)
            {
                var specifications = await db.ProductSpecifications.AsNoTracking()
                    .Where(item => item.ProductId == atr.ProductId && item.TestStage == atr.Stage)
                    .Select(item => new { item.Id, item.SpecificationNumber }).ToListAsync();
                productIds.UnionWith(specifications.Select(item => item.Id));
                productNumbers.UnionWith(specifications.Select(item => item.SpecificationNumber).Where(number => !string.IsNullOrWhiteSpace(number)));
            }
        }
        else if (response.MaterialBatchId.HasValue || response.MaterialSamplingId.HasValue)
        {
            var batchId = response.MaterialBatchId ?? await db.MaterialSamplings.AsNoTracking()
                .Where(item => item.Id == response.MaterialSamplingId).Select(item => (Guid?)item.MaterialBatchId).SingleOrDefaultAsync();
            var materialId = await db.MaterialBatches.AsNoTracking().Where(item => item.Id == batchId)
                .Select(item => (Guid?)item.MaterialId).SingleOrDefaultAsync();
            var specifications = await db.MaterialSpecifications.AsNoTracking().Where(item => item.MaterialId == materialId)
                .Select(item => new { item.Id, item.SpecificationNumber }).ToListAsync();
            materialIds.UnionWith(specifications.Select(item => item.Id));
            materialNumbers.UnionWith(specifications.Select(item => item.SpecificationNumber).Where(number => !string.IsNullOrWhiteSpace(number)));
        }
        else
        {
            // Template/specification editing and routine forms have no product or material batch context.
            return sections.ToDictionary(section => section.Id, section => section.Value);
        }
        return sections.ToDictionary(section => section.Id, section =>
            (!section.ProductSpecificationId.HasValue && !section.MaterialSpecificationId.HasValue)
            || (section.ProductSpecificationId.HasValue && !section.MaterialSpecificationId.HasValue
                && (productIds.Contains(section.ProductSpecificationId.Value) || productNumbers.Contains(section.ProductNumber ?? "")))
            || (section.MaterialSpecificationId.HasValue && !section.ProductSpecificationId.HasValue
                && (materialIds.Contains(section.MaterialSpecificationId.Value) || materialNumbers.Contains(section.MaterialNumber ?? "")))
                ? section.Value : null);
    }

    public static async Task Apply(ApplicationDbContext db, FormDto form, Guid responseId)
    {
        var response = await db.Responses.AsNoTracking().SingleAsync(item => item.Id == responseId);
        var values = await Values(db, response);
        foreach (var section in form.Sections) section.Value = values.GetValueOrDefault(section.Id);
    }
}
