using System.Globalization;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.QcWorksheets;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace APP.Services.QcWorksheets;

/// <summary>
/// Builds certificates. Generation is a system act, not a user one.
/// <para>
/// Kept out of <c>CoaRepository</c> for the same reason <see cref="IQcOosDetectionService"/> is
/// kept out of the worksheet repository: the repository serves the two user actions (issue,
/// revise), while deciding whether a round has earned a certificate at all is a separate concern
/// that has to be independently testable and callable from two different workflow hooks.
/// </para>
/// </summary>
public interface IQcCoaGenerationService
{
    /// <summary>
    /// Runs the strict-hold gate for a round and, if it passes, generates a Draft certificate.
    /// Returns null when the gate does not pass or a live certificate already exists — both are
    /// ordinary outcomes, not errors, because this is called speculatively after every review and
    /// every OOS closure.
    /// </summary>
    Task<Coa> TryGenerateAsync(Guid testRequestId, Guid userId);

    /// <summary>
    /// Whether the round currently satisfies the strict-hold rule. Exposed so the revision path
    /// can refuse to recompute rows from a round that has fallen back out of the gate.
    /// </summary>
    Task<CoaHoldEvaluation> EvaluateHoldAsync(Guid testRequestId);

    /// <summary>
    /// Assembles a fresh set of rows and header values for the round, without persisting
    /// anything. The revision path uses this to rebuild a certificate from current data.
    /// </summary>
    Task<Coa> BuildAsync(Guid testRequestId, Guid userId);
}

/// <summary>Why the strict-hold gate did or did not open.</summary>
/// <param name="Satisfied">True when a certificate may be generated.</param>
/// <param name="Reason">In words, for the revision endpoint's error and for the log. Null when satisfied.</param>
public readonly record struct CoaHoldEvaluation(bool Satisfied, string Reason)
{
    internal static CoaHoldEvaluation Pass() => new(true, null);
    internal static CoaHoldEvaluation Fail(string reason) => new(false, reason);
}

/// <inheritdoc />
public class QcCoaGenerationService(
    ApplicationDbContext context,
    ILogger<QcCoaGenerationService> logger) : IQcCoaGenerationService
{
    public async Task<Coa> TryGenerateAsync(Guid testRequestId, Guid userId)
    {
        if (testRequestId == Guid.Empty)
            return null;

        // A round holds at most one live certificate. A Draft already waiting to be issued, or an
        // Issued one, means this round's certificate exists — the path to a different one is an
        // explicit revision, never a second automatic generation.
        var alreadyLive = await context.Coas
            .AsNoTracking()
            .AnyAsync(item => item.TestRequestId == testRequestId
                && item.Status != CoaStatus.Superseded);

        if (alreadyLive)
            return null;

        var hold = await EvaluateHoldAsync(testRequestId);
        if (!hold.Satisfied)
        {
            logger.LogInformation(
                "COA generation withheld for test request {TestRequestId}: {Reason}",
                testRequestId, hold.Reason);

            return null;
        }

        var coa = await BuildAsync(testRequestId, userId);
        if (coa is null)
            return null;

        context.Coas.Add(coa);
        await context.SaveChangesAsync();

        logger.LogInformation(
            "COA {CertificateCode} generated automatically at Draft for test request "
            + "{TestRequestId} ({RowCount} row(s), overall {Verdict}).",
            coa.CertificateCode, testRequestId, coa.Rows.Count,
            coa.OverallComplies ? "COMPLIES" : "DOES NOT COMPLY");

        return coa;
    }

    // -----------------------------------------------------------------------
    // The strict-hold gate
    // -----------------------------------------------------------------------

    /// <summary>
    /// The locked combination rule from lifecycle-and-governance.md, as a hard gate.
    /// <para>
    /// No certificate — not even an interim Chemical-only one — until <b>every</b> required
    /// WorksheetInstance for <b>every</b> Subject in the round is Reviewed, and no OOS case
    /// anywhere in the round is unclosed. Microbial incubation routinely finishes three to seven
    /// days after a chemical assay; releasing a chemical-only document in that window would imply
    /// a release the microbial track has not yet supported.
    /// </para>
    /// </summary>
    public async Task<CoaHoldEvaluation> EvaluateHoldAsync(Guid testRequestId)
    {
        var round = await context.QcTestRequests
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == testRequestId);

        if (round is null)
            return CoaHoldEvaluation.Fail("The test request does not exist.");

        var subjects = await context.QcTestRequestSubjects
            .AsNoTracking()
            .Where(item => item.TestRequestId == testRequestId)
            .ToListAsync();

        if (subjects.Count == 0)
            return CoaHoldEvaluation.Fail("The test request has no subjects to certify.");

        var subjectIds = subjects.Select(item => item.Id).ToList();

        var instances = await context.QcWorksheetInstances
            .AsNoTracking()
            .Where(item => subjectIds.Contains(item.TestRequestSubjectId))
            .ToListAsync();

        if (instances.Count == 0)
            return CoaHoldEvaluation.Fail("The test request has no worksheets to certify.");

        // The required set comes from the round's PINNED Specification version row, resolved by
        // id. Nothing walks SupersedesId, so a Specification that gained a second track after
        // this round was raised cannot retroactively demand work the round never materialized.
        var requiredTemplateIds = await context.QcSpecificationWorksheetLinks
            .AsNoTracking()
            .Where(item => item.SpecificationId == round.SpecificationId)
            .Select(item => item.WorksheetTemplateId)
            .Distinct()
            .ToListAsync();

        foreach (var subject in subjects)
        {
            var own = instances
                .Where(item => item.TestRequestSubjectId == subject.Id)
                .ToList();

            if (own.Count == 0)
                return CoaHoldEvaluation.Fail(
                    $"Subject '{subject.SubjectRef}' has no worksheets.");

            // A fresh-resample Subject carries only the one worksheet the retest re-runs, by
            // design (OosCaseRepository.BuildResampleSubject). Demanding the full link set of it
            // would hold every FreshResample round open forever, so its required set is exactly
            // what it carries.
            var isResample = own.TrueForAll(item => item.RetestOfInstanceId.HasValue);

            if (!isResample)
            {
                var missing = requiredTemplateIds
                    .Where(templateId => own.TrueForAll(item => item.WorksheetTemplateId != templateId))
                    .ToList();

                if (missing.Count > 0)
                    return CoaHoldEvaluation.Fail(
                        $"Subject '{subject.SubjectRef}' is missing {missing.Count} worksheet(s) "
                        + "required by the pinned specification.");
            }

            var unfinished = own.Find(item => !IsFinished(item.Status));
            if (unfinished is not null)
                return CoaHoldEvaluation.Fail(
                    $"Subject '{subject.SubjectRef}' has a worksheet that is still "
                    + $"{unfinished.Status}. Every worksheet in the round must be Reviewed.");
        }

        // The single source of truth for the OOS hold, shared with OosCaseRepository rather than
        // re-derived here.
        var openCases = await QcReleaseHold.CountOpenCasesAsync(context, testRequestId);
        if (openCases > 0)
            return CoaHoldEvaluation.Fail(
                $"{openCases} OOS case(s) on this round are still open. A certificate cannot be "
                + "generated until every one is closed by a QA disposition.");

        return CoaHoldEvaluation.Pass();
    }

    /// <summary>
    /// Reviewed, or Locked because an earlier certificate already froze it. Both mean the work is
    /// finished and signed for.
    /// </summary>
    private static bool IsFinished(WorksheetInstanceStatus status) =>
        status is WorksheetInstanceStatus.Reviewed or WorksheetInstanceStatus.Locked;

    // -----------------------------------------------------------------------
    // Assembly
    // -----------------------------------------------------------------------

    public async Task<Coa> BuildAsync(Guid testRequestId, Guid userId)
    {
        var round = await context.QcTestRequests
            .AsNoTracking()
            .Include(item => item.Specification)
            .SingleOrDefaultAsync(item => item.Id == testRequestId);

        if (round is null)
            return null;

        // Loaded without Include: MaterialBatch reaches Material, which auto-includes its own
        // Batches, and EF refuses that cycle in a no-tracking query. The two batch-borne header
        // values are projected separately below instead.
        var subjects = await context.QcTestRequestSubjects
            .AsNoTracking()
            .Where(item => item.TestRequestId == testRequestId)
            .OrderBy(item => item.CreatedAt)
            .ThenBy(item => item.SubjectRef)
            .ToListAsync();

        if (subjects.Count == 0)
            return null;

        var subjectIds = subjects.Select(item => item.Id).ToList();

        var instances = await context.QcWorksheetInstances
            .AsNoTracking()
            .Where(item => subjectIds.Contains(item.TestRequestSubjectId))
            .ToListAsync();

        var instanceIds = instances.Select(item => item.Id).ToList();

        // Only IncludeOnCoa Characteristics print. A worksheet legitimately carries internal
        // working fields — raw_area, dilution_factor — that feed a calculation and never appear
        // on the certificate; only the resolved Result field they produce does.
        var characteristics = await context.QcSpecificationCharacteristics
            .AsNoTracking()
            .Where(item => item.SpecificationId == round.SpecificationId && item.IncludeOnCoa)
            .ToListAsync();

        var values = await context.QcWorksheetFieldValues
            .AsNoTracking()
            .Where(item => instanceIds.Contains(item.WorksheetInstanceId))
            .ToListAsync();

        // Closed cases decide which of an original and its retest the certificate may draw from —
        // the disposition record is what says so (lifecycle-and-governance.md).
        var dispositions = await context.QcOosCases
            .AsNoTracking()
            .Where(item => item.Status == OosCaseStatus.Closed
                && instanceIds.Contains(item.WorksheetInstanceId))
            .ToListAsync();

        var shape = round.Type == TestRequestType.RoutineEnvironmental
            ? CoaCertificateShape.EnvironmentalMonitoringReport
            : CoaCertificateShape.CertificateOfAnalysis;

        // A multi-point round is sampled over a window rather than at an instant, so the header's
        // Sample Date is when collection began.
        var collected = subjects
            .Where(item => item.CollectedAt.HasValue)
            .Select(item => item.CollectedAt.Value)
            .ToList();

        var coa = new Coa
        {
            Id = Guid.NewGuid(),
            TestRequestId = testRequestId,
            CertificateCode = await AllocateCodeAsync(shape),
            CertificateShape = shape,
            Status = CoaStatus.Draft,
            RevisionNumber = 1,

            // The round's own pin, copied rather than resolved forward.
            SpecificationId = round.SpecificationId,
            SpecificationVersion = round.SpecificationVersion,
            SpecificationCode = round.Specification?.Code,

            SampleDate = collected.Count == 0 ? null : collected.Min(),
            TestCompletionDate = await ResolveTestCompletionAsync(instanceIds),
            CreatedAt = DateTime.UtcNow,
            CreatedById = userId
        };

        ApplyHeaderIdentity(coa, round, subjects, shape, await ResolveBatchFactsAsync(subjects));

        coa.Rows = BuildRows(coa.Id, subjects, instances, characteristics, values, dispositions, userId);
        coa.OverallComplies = coa.Rows.TrueForAll(row => row.Complies);

        return coa;
    }

    /// <summary>What the linked batch record contributes to a Certificate of Analysis header.</summary>
    private readonly record struct BatchFacts(string Name, DateTime? ManufacturingDate, DateTime? ExpiryDate);

    /// <summary>
    /// Reads the batch-borne header values by projection rather than by navigation, which keeps
    /// the query free of the Material/Batches cycle and pulls only the four scalars the header
    /// actually prints.
    /// <para>
    /// Only a single-Subject round has one batch to describe. A multi-Subject round's batch
    /// identity belongs to its row-groups, so nothing is read here for one.
    /// </para>
    /// </summary>
    private async Task<BatchFacts> ResolveBatchFactsAsync(List<TestRequestSubject> subjects)
    {
        if (subjects.Count != 1)
            return default;

        var subject = subjects[0];

        if (subject.MaterialBatchId.HasValue)
        {
            var batch = await context.MaterialBatches
                .AsNoTracking()
                .Where(item => item.Id == subject.MaterialBatchId.Value)
                .Select(item => new BatchFacts(
                    item.Material.Name, item.ManufacturingDate, item.ExpiryDate))
                .FirstOrDefaultAsync();

            if (batch != default)
                return batch;
        }

        if (subject.BatchManufacturingRecordId.HasValue)
        {
            var record = await context.BatchManufacturingRecords
                .AsNoTracking()
                .Where(item => item.Id == subject.BatchManufacturingRecordId.Value)
                .Select(item => new BatchFacts(
                    item.ProductionScheduleProduct.Product.Name,
                    item.ManufacturingDate,
                    item.ExpiryDate))
                .FirstOrDefaultAsync();

            if (record != default)
                return record;
        }

        return default;
    }

    /// <summary>
    /// The shape-specific part of the header. A Certificate of Analysis names the product and its
    /// batch; a Monitoring Report names the area. Neither borrows the other's fields.
    /// </summary>
    private static void ApplyHeaderIdentity(
        Coa coa,
        TestRequest round,
        List<TestRequestSubject> subjects,
        CoaCertificateShape shape,
        BatchFacts batch)
    {
        var single = subjects.Count == 1 ? subjects[0] : null;

        if (shape == CoaCertificateShape.EnvironmentalMonitoringReport)
        {
            coa.AreaOrRoom = single is not null
                ? Describe(single)
                : $"{subjects.Count} monitored locations";

            return;
        }

        // The batch record names the material or product; the Specification's own name is the
        // fallback for a round whose Subject carries no batch link (a Water round, or a Material
        // round raised against a batch number that is not yet a MaterialBatch row).
        coa.ProductOrMaterialName = string.IsNullOrWhiteSpace(batch.Name)
            ? round.Specification?.Name
            : batch.Name;

        coa.BatchNumber = single?.SubjectRef;
        coa.ManufacturingDate = batch.ManufacturingDate;
        coa.ExpiryDate = batch.ExpiryDate;
    }

    private static string Describe(TestRequestSubject subject) =>
        string.IsNullOrWhiteSpace(subject.SubjectLabel)
            ? subject.SubjectRef
            : $"{subject.SubjectLabel} ({subject.SubjectRef})";

    /// <summary>
    /// When testing actually finished: the latest review signature across the round's worksheets.
    /// <para>
    /// Read from the QcApproval trail rather than from the instances themselves, because no column
    /// on a WorksheetInstance records when it was reviewed — <c>UpdatedAt</c> moves again on every
    /// later write and cannot stand in for it.
    /// </para>
    /// </summary>
    private async Task<DateTime?> ResolveTestCompletionAsync(List<Guid> instanceIds)
    {
        var signed = await context.QcApprovals
            .AsNoTracking()
            .Where(item => item.EntityType == QcApprovalEntityTypes.WorksheetInstance
                && instanceIds.Contains(item.EntityId)
                && item.Status == ApprovalStatus.Approved
                && item.ApprovalTime.HasValue)
            .Select(item => item.ApprovalTime.Value)
            .ToListAsync();

        return signed.Count == 0 ? null : signed.Max();
    }

    /// <summary>
    /// One row per (Subject × printable Characteristic), snapshotting everything the row prints.
    /// </summary>
    private static List<CoaRow> BuildRows(
        Guid coaId,
        List<TestRequestSubject> subjects,
        List<WorksheetInstance> instances,
        List<SpecificationCharacteristic> characteristics,
        List<WorksheetFieldValue> values,
        List<OosCase> dispositions,
        Guid userId)
    {
        var rows = new List<CoaRow>();

        // One (template, field) pair can carry several Characteristics differentiated only by
        // sampling point group — one EM test with a different tier per room classification. They
        // are one printed line, resolved per Subject, not several.
        var groups = characteristics
            .GroupBy(item => (item.SourceWorksheetTemplateId, item.SourceFieldKey))
            .ToList();

        foreach (var subject in subjects)
        {
            var own = instances.Where(item => item.TestRequestSubjectId == subject.Id).ToList();

            foreach (var group in groups)
            {
                var characteristic = QcCharacteristicResolver.ResolveAmong(
                    [.. group], subject.SamplingPointGroupId);

                if (characteristic is null)
                    continue;

                var source = ResolveSourceInstance(
                    own, instances, dispositions, characteristic.SourceWorksheetTemplateId,
                    characteristic.SourceFieldKey);

                // A Subject that never ran this track has nothing to print for it. This is the
                // fresh-resample case: the retest Subject re-runs one worksheet, so the tracks it
                // did not re-run simply do not appear under it.
                if (source is null)
                    continue;

                var result = values
                    .Where(item => item.WorksheetInstanceId == source.Id
                        && string.Equals(item.FieldKey, characteristic.SourceFieldKey, StringComparison.Ordinal))
                    .Select(item => item.Value)
                    .FirstOrDefault(item => !string.IsNullOrWhiteSpace(item));

                var evaluation = LimitEvaluator.Evaluate(characteristic, result);

                rows.Add(new CoaRow
                {
                    Id = Guid.NewGuid(),
                    CoaId = coaId,
                    TestRequestSubjectId = subject.Id,

                    // Snapshotted, like every other value on the row: the heading a group prints
                    // must not change because somebody relabelled the Subject afterwards.
                    SubjectRef = subject.SubjectRef,
                    SubjectLabel = subject.SubjectLabel,

                    SpecificationCharacteristicId = characteristic.Id,
                    SourceWorksheetInstanceId = source.Id,

                    DisplayLabel = Truncate(characteristic.TestName, 255),
                    GroupName = characteristic.GroupName,
                    DisplayOrder = characteristic.DisplayOrder,
                    AcceptanceCriteria = characteristic.AcceptanceCriteria,
                    ResultValue = Truncate(result, 2000),

                    // An Alert-limit breach complies: it flags for trend review and blocks
                    // nothing. Only an Action-limit breach — or a limit that could not be judged
                    // at all — fails, and neither can reach a certificate without first having
                    // been through an OOS case and a QA disposition.
                    Complies = evaluation.Outcome is LimitOutcome.Compliant or LimitOutcome.Alert,

                    CreatedAt = DateTime.UtcNow,
                    CreatedById = userId
                });
            }
        }

        return rows
            .OrderBy(row => subjects.FindIndex(subject => subject.Id == row.TestRequestSubjectId))
            .ThenBy(row => row.GroupName ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(row => row.DisplayOrder)
            .ToList();
    }

    /// <summary>
    /// Which worksheet a row's result is taken from.
    /// <para>
    /// Normally the Subject's own instance for that template. When an OOS case on that instance
    /// and that field has been closed with a disposition that displaced the original — the retest
    /// result was accepted, or the original was invalidated — the certificate draws from the
    /// retest instead, which is exactly what "the disposition record says which one the COA is
    /// allowed to draw from" means. A ConfirmedOOS disposition leaves the original standing: the
    /// result was real, and the certificate says so.
    /// </para>
    /// <para>
    /// The retest may live under a different Subject when the Specification's policy is
    /// FreshResample, which is why the search for it spans the whole round rather than this
    /// Subject alone.
    /// </para>
    /// </summary>
    private static WorksheetInstance ResolveSourceInstance(
        List<WorksheetInstance> subjectInstances,
        List<WorksheetInstance> allInstances,
        List<OosCase> dispositions,
        Guid templateId,
        string fieldKey)
    {
        var original = subjectInstances.Find(
            item => item.WorksheetTemplateId == templateId && !item.RetestOfInstanceId.HasValue)
            ?? subjectInstances.Find(item => item.WorksheetTemplateId == templateId);

        if (original is null)
            return null;

        var displaced = dispositions.Find(item =>
            item.WorksheetInstanceId == original.Id
            && string.Equals(item.FieldKey, fieldKey, StringComparison.Ordinal)
            && item.RetestWorksheetInstanceId.HasValue
            && item.DispositionOutcome is OosDispositionOutcome.RetestAccepted
                or OosDispositionOutcome.Invalidated);

        if (displaced is null)
            return original;

        var retest = allInstances.Find(item => item.Id == displaced.RetestWorksheetInstanceId.Value);

        return retest is not null && IsFinished(retest.Status) ? retest : original;
    }

    private static string Truncate(string value, int maxLength) =>
        value is not null && value.Length > maxLength ? value[..maxLength] : value;

    // -----------------------------------------------------------------------
    // Certificate codes
    // -----------------------------------------------------------------------

    /// <summary>
    /// Allocates the next code for the shape, scoped to the calendar year — the format the real
    /// certificates use (<c>COA-2026-00842</c>). A Monitoring Report takes its own <c>EMR</c>
    /// series, because it is not a Certificate of Analysis and must not be filed as one.
    /// </summary>
    private async Task<string> AllocateCodeAsync(CoaCertificateShape shape)
    {
        var prefix = shape == CoaCertificateShape.EnvironmentalMonitoringReport ? "EMR" : "COA";
        var year = DateTime.UtcNow.Year.ToString(CultureInfo.InvariantCulture);
        var series = $"{prefix}-{year}-";

        // Counted rather than max-parsed: a revision's code carries an "-R2" suffix, so the
        // sequence is owned by the originals and a straight numeric max over the column would
        // have to strip suffixes to be correct.
        var issued = await context.Coas
            .IgnoreQueryFilters()
            .AsNoTracking()
            .CountAsync(item => item.CertificateCode.StartsWith(series)
                && item.SupersedesId == null);

        return $"{series}{(issued + 1).ToString("D5", CultureInfo.InvariantCulture)}";
    }
}
