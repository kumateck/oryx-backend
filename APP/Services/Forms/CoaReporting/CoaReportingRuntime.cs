using DOMAIN.Entities.Forms;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

#nullable enable
namespace APP.Services.Forms.CoaReporting;

/// <summary>
/// The one rule for whether a worksheet test prints on a certificate: the product or material specification's override when it has one,
/// otherwise the template field's "Report on COA" choice. Unreported tests are still tested and kept on the worksheet.
/// </summary>
public static class CoaReportingRuntime
{
    /// <summary>The fields of the response's template that are not reported on its certificate.</summary>
    public static async Task<HashSet<Guid>> HiddenFields(ApplicationDbContext db, Response response)
    {
        var fields = await db.FormFields.AsNoTracking().IgnoreQueryFilters()
            .Where(field => field.FormSection.FormId == response.FormId)
            .Select(field => new { field.Id, field.ReportOnCoa }).ToListAsync();
        var overrides = await Overrides(db, response);
        return fields.Where(field => !(overrides.TryGetValue(field.Id, out var chosen) ? chosen : field.ReportOnCoa))
            .Select(field => field.Id).ToHashSet();
    }

    /// <summary>The specification overrides that apply to a product or material response, keyed by field.</summary>
    private static async Task<Dictionary<Guid, bool>> Overrides(ApplicationDbContext db, Response response)
    {
        if (response.MaterialBatchId.HasValue || response.MaterialSamplingId.HasValue) return await MaterialOverrides(db, response);
        if (!response.BatchManufacturingRecordId.HasValue) return [];
        var atr = await db.AnalyticalTestRequests.AsNoTracking()
            .Where(item => item.BatchManufacturingRecordId == response.BatchManufacturingRecordId && item.ProductionActivityStepId == response.ProductionActivityStepId)
            .OrderByDescending(item => item.CreatedAt).Select(item => new { item.ProductionScheduleProduct.ProductId, item.Stage }).FirstOrDefaultAsync();
        if (atr is null) return [];
        var spec = await db.ProductSpecifications.AsNoTracking()
            .Where(item => item.ProductId == atr.ProductId && item.TestStage == atr.Stage
                && (item.MicrobialFormId == response.FormId || item.FormId == response.FormId))
            .Select(item => (Guid?)item.Id).FirstOrDefaultAsync();
        if (spec is null) return [];
        return await db.SpecificationCoaReportings.AsNoTracking().Where(item => item.ProductSpecificationId == spec)
            .ToDictionaryAsync(item => item.FormFieldId, item => item.ReportOnCoa);
    }

    /// <summary>The overrides of the material's specification that uses the response's template.</summary>
    private static async Task<Dictionary<Guid, bool>> MaterialOverrides(ApplicationDbContext db, Response response)
    {
        var batchId = response.MaterialBatchId ?? await db.MaterialSamplings.AsNoTracking().Where(item => item.Id == response.MaterialSamplingId)
            .Select(item => (Guid?)item.MaterialBatchId).SingleOrDefaultAsync();
        var materialId = await db.MaterialBatches.AsNoTracking().Where(item => item.Id == batchId).Select(item => (Guid?)item.MaterialId).SingleOrDefaultAsync();
        if (materialId is null) return [];
        var spec = await db.MaterialSpecifications.AsNoTracking()
            .Where(item => item.MaterialId == materialId && (item.MicrobialFormId == response.FormId || item.FormId == response.FormId))
            .Select(item => (Guid?)item.Id).FirstOrDefaultAsync();
        if (spec is null) return [];
        return await db.SpecificationCoaReportings.AsNoTracking().Where(item => item.MaterialSpecificationId == spec)
            .ToDictionaryAsync(item => item.FormFieldId, item => item.ReportOnCoa);
    }
}
