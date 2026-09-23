using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

internal static class MaterialSampleBinding
{
    internal static async Task<Result<Guid?>> ResolveAsync(ApplicationDbContext context,
        Guid? batchId, Guid? requestedSampleId)
    {
        if (!batchId.HasValue)
            return requestedSampleId.HasValue
                ? Error.Validation("MaterialSample.Batch", "Sample requires a material batch.")
                : Result.Success<Guid?>(null);
        if (requestedSampleId.HasValue)
        {
            var requested = await context.MaterialSamplings.FirstOrDefaultAsync(item =>
                item.Id == requestedSampleId && item.MaterialBatchId == batchId);
            if (requested is null)
                return Error.Validation("MaterialSample.Context",
                    "Requested sampling does not belong to this material batch.");
            return requested.ChemicalArdId.HasValue ? requested.Id : null;
        }
        var latest = await context.MaterialSamplings
            .Where(item => item.MaterialBatchId == batchId)
            .OrderByDescending(item => item.SampleDate)
            .ThenByDescending(item => item.CreatedAt)
            .FirstOrDefaultAsync();
        return latest?.ChemicalArdId.HasValue == true ? latest.Id : null;
    }
}
