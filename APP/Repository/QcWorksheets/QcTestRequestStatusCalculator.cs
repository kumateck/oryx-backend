using DOMAIN.Entities.QcWorksheets;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

namespace APP.Repository.QcWorksheets;

/// <summary>
/// Keeps a round's Status in step with the worksheets underneath it.
/// <para>
/// The round has no endpoints of its own between Sampled and Released: its status is
/// <b>derived</b> from its WorksheetInstances every time one of them moves, which is why a
/// worksheet returned for correction pulls the whole round back to InTesting without anyone
/// having to remember to do it.
/// </para>
/// <para>
/// Two states are deliberately never written here. <see cref="TestRequestStatus.Draft"/> is
/// left alone because leaving Draft is what recording the sample means, not something the
/// worksheets decide. <see cref="TestRequestStatus.Released"/> and
/// <see cref="TestRequestStatus.Rejected"/> are terminal dispositions that depend on OOS
/// outcomes (Milestone 4) and certificate issue (Milestone 5) — this milestone stops at
/// UnderReview.
/// </para>
/// </summary>
internal static class QcTestRequestStatusCalculator
{
    /// <summary>
    /// Recomputes and saves the round's status from the worksheets under it. The caller is
    /// expected to have already saved whatever instance change triggered this.
    /// </summary>
    internal static async Task RecalculateAsync(ApplicationDbContext context, Guid testRequestId)
    {
        var request = await context.QcTestRequests
            .SingleOrDefaultAsync(item => item.Id == testRequestId);

        if (request is null)
            return;

        // Draft has not been sampled yet, and the two terminal dispositions belong to later
        // milestones. Neither is the worksheets' to decide.
        if (request.Status is TestRequestStatus.Draft
            or TestRequestStatus.Released
            or TestRequestStatus.Rejected)
            return;

        var instances = await context.QcWorksheetInstances
            .AsNoTracking()
            .Where(instance => context.QcTestRequestSubjects
                .Any(subject => subject.Id == instance.TestRequestSubjectId
                    && subject.TestRequestId == testRequestId))
            .Select(instance => new { instance.Status, instance.AssignedToId })
            .ToListAsync();

        if (instances.Count == 0)
            return;

        var status = Derive(instances.Select(item => (item.Status, item.AssignedToId)).ToList());

        if (request.Status == status)
            return;

        request.Status = status;
        request.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// The derivation itself, separated so it is testable without a database.
    /// </summary>
    internal static TestRequestStatus Derive(
        IReadOnlyCollection<(WorksheetInstanceStatus Status, Guid? AssignedToId)> instances)
    {
        var allSubmitted = instances.All(item => item.Status >= WorksheetInstanceStatus.Submitted);
        var anyReviewed = instances.Any(item => item.Status >= WorksheetInstanceStatus.Reviewed);
        var anyStarted = instances.Any(item => item.Status >= WorksheetInstanceStatus.InProgress);
        var anyAssigned = instances.Any(item => item.AssignedToId.HasValue);

        // Review has begun on a round whose results are all in.
        if (allSubmitted && anyReviewed) return TestRequestStatus.UnderReview;

        // Every worksheet is submitted, nobody has reviewed one yet.
        if (allSubmitted) return TestRequestStatus.ResultsComplete;

        // Work is under way — including the case where some worksheets are already reviewed
        // while others are still being filled in.
        if (anyStarted) return TestRequestStatus.InTesting;

        if (anyAssigned) return TestRequestStatus.Assigned;

        return TestRequestStatus.Sampled;
    }
}
