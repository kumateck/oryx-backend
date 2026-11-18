using DOMAIN.Entities.Materials.Batch;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

namespace APP.Services.Materials;

/// <summary>
/// A material retest is tested like an analytical test request: the retest record carries the test's progress, and the
/// batch stays held (Frozen) until the retest is released. The material test's steps call this before moving a batch
/// status; when the batch's current sample was taken for a retest, the retest moves instead and the batch is left alone.
/// </summary>
public static class MaterialRetestTesting
{
    private static readonly MaterialRetestStatus[] Active =
        [MaterialRetestStatus.Acknowledged, MaterialRetestStatus.Assigned, MaterialRetestStatus.Testing,
         MaterialRetestStatus.TestTaken, MaterialRetestStatus.Checked];

    /// <summary>The retest the batch's newest sample was taken for, while it is being tested; otherwise null.</summary>
    public static async Task<MaterialRetestRequest> ActiveAsync(ApplicationDbContext context, Guid batchId)
    {
        var retestId = await context.MaterialSamplings.IgnoreQueryFilters()
            .Where(item => item.MaterialBatchId == batchId && item.DeletedAt == null)
            .OrderByDescending(item => item.SampleDate).ThenByDescending(item => item.CreatedAt)
            .Select(item => item.MaterialRetestRequestId).FirstOrDefaultAsync();
        if (!retestId.HasValue) return null;
        var retest = await context.MaterialRetestRequests.SingleOrDefaultAsync(item => item.Id == retestId);
        return retest is not null && Active.Contains(retest.Status) ? retest : null;
    }

    /// <returns>True when the batch is under a retest, whose status now records <paramref name="step"/>.</returns>
    public static async Task<bool> AdvanceAsync(ApplicationDbContext context, Guid batchId, MaterialRetestStatus step, Guid? actorId, string detail)
    {
        var retest = await ActiveAsync(context, batchId);
        if (retest is null) return false;
        retest.Status = step;
        retest.UpdatedAt = DateTime.UtcNow;
        retest.LastUpdatedById = actorId;
        context.MaterialRetestEvents.Add(new MaterialRetestEvent
        {
            Id = Guid.NewGuid(), MaterialRetestRequestId = retest.Id, Action = step.ToString(), Detail = detail,
            ActorId = actorId, OccurredAt = DateTime.UtcNow,
        });
        return true;
    }
}
