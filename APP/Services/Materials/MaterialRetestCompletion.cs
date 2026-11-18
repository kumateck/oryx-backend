using DOMAIN.Entities.Materials.Batch;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

namespace APP.Services.Materials;

/// <summary>
/// Ends a material retest when the batch's material test reaches its outcome. The test runs against the newest sample,
/// so the retest is the one that sample was taken for. A complying result extends the batch's expiry to the date QA
/// proposed and returns the batch to stock; a failing one rejects the retest.
/// </summary>
public static class MaterialRetestCompletion
{
    /// <returns>True when the batch's current test belonged to a retest, which is now closed.</returns>
    public static async Task<bool> ApplyAsync(ApplicationDbContext context, MaterialBatch batch, bool complies, Guid? actorId)
    {
        var retest = await MaterialRetestTesting.ActiveAsync(context, batch.Id);
        if (retest is null) return false;
        var now = DateTime.UtcNow;
        retest.Status = complies ? MaterialRetestStatus.Completed : MaterialRetestStatus.Rejected;
        retest.CompletedAt = now;
        retest.CompletedById = actorId;
        if (complies)
        {
            if (retest.ProposedExpiryDate.HasValue) batch.ExpiryDate = retest.ProposedExpiryDate;
            batch.Status = BatchStatus.Available;
            batch.UpdatedAt = now;
            batch.LastUpdatedById = actorId;
        }
        context.MaterialRetestEvents.Add(new MaterialRetestEvent
        {
            Id = Guid.NewGuid(), MaterialRetestRequestId = retest.Id, ActorId = actorId, OccurredAt = now,
            Action = complies ? "ExpiryExtended" : "TestFailed",
            Detail = complies ? $"The material test was approved; expiry extended to {retest.ProposedExpiryDate:yyyy-MM-dd} and the batch returned to stock."
                : "The material test did not comply; the batch is rejected.",
        });
        return true;
    }
}
