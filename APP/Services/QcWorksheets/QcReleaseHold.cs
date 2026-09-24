using DOMAIN.Entities.QcWorksheets;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

namespace APP.Services.QcWorksheets;

/// <summary>
/// The one definition of "an OOS case is holding this round back".
/// <para>
/// Extracted so <c>OosCaseRepository.IsReleaseBlocked</c> and the <c>BlocksRelease</c> /
/// <c>BlockingOosCases</c> pair on the TestRequest detail share a single implementation rather
/// than each deriving the rule. Two places deciding what counts as an open case is two places
/// for them to disagree — and the disagreement would surface as a test-request screen telling a
/// user a round is clear while the release gate still holds it, or the reverse.
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
}
