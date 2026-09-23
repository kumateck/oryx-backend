using DOMAIN.Entities.QcWorksheets;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

namespace APP.Services.QcWorksheets;

/// <summary>
/// The one definition of "an OOS case is holding this round back".
/// <para>
/// Extracted so every reader of that rule shares a single implementation rather than each
/// deriving it: <c>OosCaseRepository.IsReleaseBlocked</c>, the <c>BlocksRelease</c> /
/// <c>BlockingOosCases</c> pair on the TestRequest detail, and the COA engine's strict-hold
/// gate. Two places deciding what counts as an open case is two places for them to disagree —
/// and the disagreement would surface either as a certificate issued for a round that is still
/// under investigation, or as a test-request screen telling a user a round is clear while the
/// release gate still holds it.
/// </para>
/// <para>
/// "Open" means anything that is not <see cref="OosCaseStatus.Closed"/>: Open,
/// InvestigationInProgress, RetestRequested and PendingQaDisposition all block. Only a QA
/// disposition releases the hold.
/// </para>
/// </summary>
public static class QcReleaseHold
{
    /// <summary>
    /// Every unclosed OOS case sitting anywhere under the given round, oldest first.
    /// <para>
    /// Returned as a queryable rather than a list so callers project only the columns they
    /// need, in one indexed read. The subject/instance hops are correlated subqueries rather
    /// than joins, which keeps the OosCase row set the only thing materialized.
    /// </para>
    /// </summary>
    public static IQueryable<OosCase> OpenCasesFor(ApplicationDbContext context, Guid testRequestId) =>
        context.QcOosCases
            .AsNoTracking()
            .Where(item => item.Status != OosCaseStatus.Closed
                && context.QcWorksheetInstances.Any(instance =>
                    instance.Id == item.WorksheetInstanceId
                    && context.QcTestRequestSubjects.Any(subject =>
                        subject.Id == instance.TestRequestSubjectId
                        && subject.TestRequestId == testRequestId)))
            .OrderBy(item => item.OpenedAt);

    /// <summary>How many unclosed OOS cases sit anywhere under the given round.</summary>
    public static Task<int> CountOpenCasesAsync(ApplicationDbContext context, Guid testRequestId) =>
        OpenCasesFor(context, testRequestId).CountAsync();

    /// <summary>Whether any unclosed OOS case is holding the round.</summary>
    public static async Task<bool> IsHeldAsync(ApplicationDbContext context, Guid testRequestId) =>
        await CountOpenCasesAsync(context, testRequestId) > 0;
}
