using DOMAIN.Entities.AnalyticalTestRequests;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.QualityRoutines;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

internal static class QualityAnalysisReadiness
{
    internal static async Task<Result<bool>> MaterialAsync(ApplicationDbContext context,
        Guid batchId)
    {
        var sample = await context.MaterialSamplings
            .Where(item => item.MaterialBatchId == batchId)
            .OrderByDescending(item => item.SampleDate)
            .ThenByDescending(item => item.CreatedAt)
            .FirstOrDefaultAsync();
        if (sample is null)
            return Error.Conflict("QualityAnalysis.Sample",
                "A material batch requires a sampling record before QC release.");
        return await MaterialSampleAsync(context, sample);
    }

    internal static async Task<Result<bool>> MaterialSampleAsync(
        ApplicationDbContext context,
        DOMAIN.Entities.MaterialSampling.MaterialSampling sample)
    {
        var batchId = sample.MaterialBatchId;
        var batch = await context.MaterialBatches
            .Where(item => item.Id == batchId)
            .Select(item => new { item.MaterialId }).FirstOrDefaultAsync();
        if (batch is null)
            return Error.NotFound("QualityAnalysis.Batch", "Material batch was not found.");
        var forms = await MaterialFormsAsync(context, batch.MaterialId, sample.ChemicalArdId,
            sample.MicrobialArdId, sample.MicrobialRequired);
        if (forms.IsFailure) return forms.Errors;
        var boundSampleId = sample.ChemicalArdId.HasValue ? sample.Id : (Guid?)null;
        var responses = await context.Responses
            .Where(item => item.MaterialBatchId == batchId &&
                item.MaterialSamplingId == boundSampleId).ToListAsync();
        return Complete(forms.Value, responses);
    }

    internal static async Task<Result<bool>> ProductStageAsync(ApplicationDbContext context,
        AnalyticalTestRequest atr)
    {
        var productId = await context.ProductionScheduleProducts
            .Where(item => item.Id == atr.ProductionScheduleProductId)
            .Select(item => item.ProductId).FirstOrDefaultAsync();
        if (productId == Guid.Empty)
            return Error.NotFound("QualityAnalysis.Product", "ATR product was not found.");
        var forms = await ProductFormsAsync(context, productId, atr.Stage,
            atr.ChemicalArdId, atr.MicrobialArdId, atr.MicrobialRequired);
        if (forms.IsFailure) return forms.Errors;
        var responses = await context.Responses.Where(item =>
            item.BatchManufacturingRecordId == atr.BatchManufacturingRecordId &&
            item.ProductionActivityStepId == atr.ProductionActivityStepId).ToListAsync();
        return Complete(forms.Value, responses);
    }

    internal static async Task<Result<List<Guid>>> MaterialFormsAsync(
        ApplicationDbContext context, Guid materialId, Guid? chemicalArdId,
        Guid? microbialArdId, bool microbialRequired)
    {
        var ards = await context.MaterialAnalyticalRawData
            .Where(item => item.MaterialStandardTestProcedure.MaterialId == materialId &&
                (!chemicalArdId.HasValue || item.Id == chemicalArdId ||
                    microbialArdId.HasValue && item.Id == microbialArdId))
            .Select(item => new { item.Id, item.FormId, item.AnalysisType })
            .ToListAsync();
        return Forms(ards.Select(item => (item.Id, item.FormId, item.AnalysisType)),
            chemicalArdId, microbialArdId, microbialRequired);
    }

    internal static async Task<Result<List<Guid>>> ProductFormsAsync(
        ApplicationDbContext context, Guid productId, TestStage stage,
        Guid? chemicalArdId, Guid? microbialArdId, bool microbialRequired)
    {
        var ards = await context.ProductAnalyticalRawData
            .Where(item => item.ProductStandardTestProcedure.ProductId == productId &&
                item.Stage == stage && (!chemicalArdId.HasValue ||
                    item.Id == chemicalArdId ||
                    microbialArdId.HasValue && item.Id == microbialArdId))
            .Select(item => new { item.Id, item.FormId, item.AnalysisType })
            .ToListAsync();
        return Forms(ards.Select(item => (item.Id, item.FormId, item.AnalysisType)),
            chemicalArdId, microbialArdId, microbialRequired);
    }

    private static Result<List<Guid>> Forms(
        IEnumerable<(Guid Id, Guid FormId, AnalysisType Type)> items,
        Guid? chemicalArdId, Guid? microbialArdId, bool microbialRequired)
    {
        var all = items.ToList();
        var chemical = chemicalArdId.HasValue
            ? all.Where(item => item.Id == chemicalArdId &&
                item.Type == AnalysisType.Chemical).ToList()
            : all.Where(item => item.Type == AnalysisType.Chemical).ToList();
        var microbial = microbialRequired
            ? all.Where(item => item.Id == microbialArdId &&
                item.Type == AnalysisType.Microbial).ToList()
            : [];
        if (chemical.Count != 1 || microbialRequired && microbial.Count != 1)
            return Error.Conflict("QualityAnalysis.ArdSnapshot",
                "The required Chemical or Microbial ARD snapshot is missing or ambiguous.");
        return microbialRequired
            ? new List<Guid> { chemical[0].FormId, microbial[0].FormId }
            : new List<Guid> { chemical[0].FormId };
    }

    private static bool Complete(List<Guid> formIds, List<Response> responses) =>
        formIds.Distinct().Count() == formIds.Count &&
        formIds.All(formId => responses.Any(item =>
            item.FormId == formId && item.Approved && !item.Rejected));
}
