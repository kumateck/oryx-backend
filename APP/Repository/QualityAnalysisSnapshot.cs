using DOMAIN.Entities.AnalyticalTestRequests;
using DOMAIN.Entities.QualityRoutines;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

internal record QualityAnalysisSelection(Guid ChemicalArdId, Guid? MicrobialArdId,
    bool MicrobialRequired);

internal static class QualityAnalysisSnapshot
{
    internal static async Task<Result<QualityAnalysisSelection>> ForMaterialAsync(
        ApplicationDbContext context, Guid materialId)
    {
        var config = await context.MicrobialRequirements
            .Where(item => item.Subject == RequirementSubject.Material &&
                item.MaterialId == materialId && item.IsVerified)
            .OrderByDescending(item => item.VerifiedAt)
            .ThenByDescending(item => item.CreatedAt)
            .FirstOrDefaultAsync();
        var ards = await context.MaterialAnalyticalRawData
            .Where(item => item.MaterialStandardTestProcedure.MaterialId == materialId &&
                item.IsVerified)
            .Select(item => new { item.Id, item.AnalysisType, item.FormId })
            .ToListAsync();
        return Select(ards.Select(item => (item.Id, item.AnalysisType, item.FormId)),
            config?.Required == true, "material");
    }

    internal static async Task<Result<QualityAnalysisSelection>> ForProductAsync(
        ApplicationDbContext context, Guid productId, TestStage stage)
    {
        var config = stage == TestStage.Finished
            ? await context.MicrobialRequirements
                .Where(item => item.Subject == RequirementSubject.Product &&
                    item.ProductId == productId && item.Stage == stage && item.IsVerified)
                .OrderByDescending(item => item.VerifiedAt)
                .ThenByDescending(item => item.CreatedAt)
                .FirstOrDefaultAsync()
            : null;
        var ards = await context.ProductAnalyticalRawData
            .Where(item => item.ProductStandardTestProcedure.ProductId == productId &&
                item.Stage == stage && item.IsVerified)
            .Select(item => new { item.Id, item.AnalysisType, item.FormId })
            .ToListAsync();
        return Select(ards.Select(item => (item.Id, item.AnalysisType, item.FormId)),
            config?.Required == true, "product stage");
    }

    private static Result<QualityAnalysisSelection> Select(
        IEnumerable<(Guid Id, AnalysisType Type, Guid FormId)> source,
        bool microbialRequired, string subject)
    {
        var items = source.ToList();
        var chemical = items.Where(item => item.Type == AnalysisType.Chemical).ToList();
        var microbial = items.Where(item => item.Type == AnalysisType.Microbial).ToList();
        if (chemical.Count != 1)
            return Error.Conflict("QualityAnalysis.ChemicalArd",
                $"Exactly one verified Chemical ARD is required for this {subject}.");
        if (microbialRequired && microbial.Count != 1)
            return Error.Conflict("QualityAnalysis.MicrobialArd",
                $"Exactly one verified Microbial ARD is required for this {subject}.");
        if (microbialRequired && chemical[0].FormId == microbial[0].FormId)
            return Error.Conflict("QualityAnalysis.Worksheet",
                "Chemical and Microbial analysis must have separate worksheet forms.");
        return new QualityAnalysisSelection(chemical[0].Id,
            microbialRequired ? microbial[0].Id : null, microbialRequired);
    }
}
