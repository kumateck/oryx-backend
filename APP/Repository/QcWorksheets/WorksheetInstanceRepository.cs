using System.Globalization;
using APP.IRepository;
using APP.Services.QcWorksheets;
using AutoMapper;
using DOMAIN.Entities.QcWorksheets;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository.QcWorksheets;

/// <summary>
/// The Test Room: one worksheet being executed against one subject of a round.
/// <para>
/// Three rules govern everything here.
/// </para>
/// <para>
/// <b>Hard version pinning</b>: the worksheet renders and validates against the template
/// version row its <c>WorksheetTemplateId</c> names, and the header resolves the Specification
/// version row the round pinned. Nothing walks <c>SupersedesId</c>, so an in-flight worksheet
/// cannot change shape underneath the analyst filling it in.
/// </para>
/// <para>
/// <b>Assignment enforcement</b>: holding the permission key is necessary but never sufficient
/// — start, enter and submit additionally require the caller to <i>be</i> the current assignee.
/// This is what makes <c>EnteredBy</c> attributable in the GxP sense.
/// </para>
/// <para>
/// <b>Hard instrument/reagent gates</b>: expired calibration or an expired reagent blocks the
/// write outright and blocks submission again afterwards. Not a warning, and no override path.
/// </para>
/// </summary>
public class WorksheetInstanceRepository(
    ApplicationDbContext context,
    IMapper mapper,
    IQcSignatureService signatureService,
    IApprovalRepository approvalRepository,
    IQcOosDetectionService oosDetection,
    IQcCoaGenerationService coaGeneration) : IWorksheetInstanceRepository
{
    private const string ModelType = QcWorksheetModelTypes.WorksheetInstance;

    /// <summary>A field plus whatever this instance has recorded against it.</summary>
    private sealed record FieldWithValues(WorksheetField Field, List<WorksheetFieldValue> Values);

    // -----------------------------------------------------------------------
    // Reads
    // -----------------------------------------------------------------------

    public async Task<Result<WorksheetInstanceDetailDto>> GetWorksheetInstance(Guid id)
    {
        var instance = await LoadDetail(id);
        return instance is null
            ? Result.Failure<WorksheetInstanceDetailDto>(QcWorksheetErrors.WorksheetInstanceNotFound(id))
            : Result.Success(await ToDetailDto(instance));
    }

    public async Task<Result<SpecificationAnalysisType>> GetAnalysisType(Guid id)
    {
        var instance = await context.QcWorksheetInstances
            .AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new { item.AnalysisType })
            .SingleOrDefaultAsync();

        return instance is null
            ? Result.Failure<SpecificationAnalysisType>(QcWorksheetErrors.WorksheetInstanceNotFound(id))
            : Result.Success(instance.AnalysisType);
    }

    /// <summary>
    /// "My Work" is scoped by assignment, not by role: an unassigned worksheet appears in
    /// nobody's queue, and reassigning one moves it between queues immediately.
    /// </summary>
    public async Task<Result<WorksheetQueueDto>> GetMyWork(
        Guid userId, SpecificationAnalysisType? analysisType)
    {
        var query = QueueQuery()
            .Where(instance => instance.AssignedToId == userId
                && instance.Status != WorksheetInstanceStatus.Reviewed
                && instance.Status != WorksheetInstanceStatus.Locked);

        if (analysisType.HasValue)
            query = query.Where(instance => instance.AnalysisType == analysisType.Value);

        var instances = await query.ToListAsync();
        var openCases = await LoadOpenOosCases(instances);

        return Result.Success(new WorksheetQueueDto
        {
            PendingCount = instances.Count(item => item.Status == WorksheetInstanceStatus.NotStarted),
            InProgressCount = instances.Count(item => item.Status == WorksheetInstanceStatus.InProgress),
            AwaitingReviewCount = instances.Count(item => item.Status == WorksheetInstanceStatus.Submitted),
            Items = instances.Select(instance => ToQueueItem(instance, openCases)).ToList()
        });
    }

    /// <summary>
    /// "Awaiting My Review". Scoped by analysis track rather than by assignment — reviewing is
    /// not the assignee's job, and the track split is what keeps a microbiologist out of the
    /// chemistry queue.
    /// </summary>
    public async Task<Result<WorksheetQueueDto>> GetReviewQueue(SpecificationAnalysisType? analysisType)
    {
        var query = QueueQuery()
            .Where(instance => instance.Status == WorksheetInstanceStatus.Submitted);

        if (analysisType.HasValue)
            query = query.Where(instance => instance.AnalysisType == analysisType.Value);

        var instances = await query.ToListAsync();
        var openCases = await LoadOpenOosCases(instances);

        return Result.Success(new WorksheetQueueDto
        {
            AwaitingReviewCount = instances.Count,
            Items = instances.Select(instance => ToQueueItem(instance, openCases)).ToList()
        });
    }

    /// <summary>
    /// The open OOS case against each worksheet in a queue, in one query rather than one per
    /// card. A worksheet can in principle carry a case per failing field; the queue flags the
    /// worksheet and links to the oldest, since the flag is "this result is under formal
    /// investigation", not a count.
    /// </summary>
    private async Task<Dictionary<Guid, Guid>> LoadOpenOosCases(List<WorksheetInstance> instances)
    {
        if (instances.Count == 0) return [];

        var ids = instances.Select(instance => instance.Id).ToList();

        var cases = await context.QcOosCases
            .AsNoTracking()
            .Where(item => ids.Contains(item.WorksheetInstanceId)
                && item.Status != OosCaseStatus.Closed)
            .Select(item => new { item.Id, item.WorksheetInstanceId, item.OpenedAt })
            .ToListAsync();

        return cases
            .GroupBy(item => item.WorksheetInstanceId)
            .ToDictionary(
                group => group.Key,
                group => group.OrderBy(item => item.OpenedAt).First().Id);
    }

    // -----------------------------------------------------------------------
    // Assignment
    // -----------------------------------------------------------------------

    public async Task<Result<WorksheetInstanceDetailDto>> Assign(
        Guid id, AssignWorksheetInstanceRequest request, Guid userId)
    {
        var instance = await context.QcWorksheetInstances.SingleOrDefaultAsync(item => item.Id == id);
        if (instance is null)
            return Result.Failure<WorksheetInstanceDetailDto>(QcWorksheetErrors.WorksheetInstanceNotFound(id));

        // Assigning is only for work nobody has begun. Moving started work is reassignment,
        // which is audited — the two are deliberately different actions with different keys.
        if (instance.Status != WorksheetInstanceStatus.NotStarted)
            return Result.Failure<WorksheetInstanceDetailDto>(
                QcWorksheetErrors.AssignRequiresNotStarted(instance.Status));

        if (!await context.Users.AnyAsync(user => user.Id == request.AssignedToId))
            return Result.Failure<WorksheetInstanceDetailDto>(
                QcWorksheetErrors.AssigneeNotFound(request.AssignedToId));

        instance.AssignedToId = request.AssignedToId;
        instance.AssignedById = userId;
        instance.AssignedAt = DateTime.UtcNow;
        instance.UpdatedAt = DateTime.UtcNow;
        instance.LastUpdatedById = userId;

        await context.SaveChangesAsync();
        await RecalculateRoundStatus(instance.TestRequestSubjectId);

        return await GetWorksheetInstance(id);
    }

    public async Task<Result<WorksheetInstanceDetailDto>> Reassign(
        Guid id, ReassignWorksheetInstanceRequest request, Guid userId)
    {
        var instance = await context.QcWorksheetInstances.SingleOrDefaultAsync(item => item.Id == id);
        if (instance is null)
            return Result.Failure<WorksheetInstanceDetailDto>(QcWorksheetErrors.WorksheetInstanceNotFound(id));

        if (instance.Status == WorksheetInstanceStatus.Locked)
            return Result.Failure<WorksheetInstanceDetailDto>(QcWorksheetErrors.ReassignRequiresUnlocked);

        if (string.IsNullOrWhiteSpace(request.Reason))
            return Result.Failure<WorksheetInstanceDetailDto>(QcWorksheetErrors.ReasonForChangeRequired);

        if (!await context.Users.AnyAsync(user => user.Id == request.AssignedToId))
            return Result.Failure<WorksheetInstanceDetailDto>(
                QcWorksheetErrors.AssigneeNotFound(request.AssignedToId));

        if (instance.AssignedToId == request.AssignedToId)
            return Result.Failure<WorksheetInstanceDetailDto>(QcWorksheetErrors.AlreadyAssignedToUser);

        var previousAssignee = instance.AssignedToId;

        // A plain audit record. Deliberately not a QcApproval and not routed through the
        // approval engine: moving work between analysts is administrative, not a signature.
        context.QcWorksheetInstanceReassignments.Add(new WorksheetInstanceReassignment
        {
            Id = Guid.NewGuid(),
            WorksheetInstanceId = instance.Id,
            FromUserId = previousAssignee,
            ToUserId = request.AssignedToId,
            ReassignedById = userId,
            ReassignedAt = DateTime.UtcNow,
            Reason = request.Reason.Trim(),
            CreatedAt = DateTime.UtcNow,
            CreatedById = userId
        });

        instance.AssignedToId = request.AssignedToId;
        instance.AssignedById = userId;
        instance.AssignedAt = DateTime.UtcNow;
        instance.UpdatedAt = DateTime.UtcNow;
        instance.LastUpdatedById = userId;

        // Status is untouched on purpose, and no existing WorksheetFieldValue is rewritten:
        // work already entered stays exactly as entered, still attributed to whoever typed it.
        await context.SaveChangesAsync();

        return await GetWorksheetInstance(id);
    }

    // -----------------------------------------------------------------------
    // Execution
    // -----------------------------------------------------------------------

    public async Task<Result<WorksheetInstanceDetailDto>> Start(Guid id, Guid userId)
    {
        var instance = await context.QcWorksheetInstances.SingleOrDefaultAsync(item => item.Id == id);
        if (instance is null)
            return Result.Failure<WorksheetInstanceDetailDto>(QcWorksheetErrors.WorksheetInstanceNotFound(id));

        var assignment = EnsureAssignee(instance, userId);
        if (!assignment.IsSuccess)
            return Result.Failure<WorksheetInstanceDetailDto>(assignment.Error);

        if (instance.Status != WorksheetInstanceStatus.NotStarted)
            return Result.Failure<WorksheetInstanceDetailDto>(
                QcWorksheetErrors.StartRequiresNotStarted(instance.Status));

        // The governance rule blocks starting as well as submitting, so anything already
        // recorded against a gated field is re-checked here too.
        var fields = await LoadFieldsWithValues(instance);
        var gate = await RunGates(fields, requireComplete: false);
        if (!gate.IsSuccess)
            return Result.Failure<WorksheetInstanceDetailDto>(gate.Error);

        instance.Status = WorksheetInstanceStatus.InProgress;
        instance.UpdatedAt = DateTime.UtcNow;
        instance.LastUpdatedById = userId;

        await context.SaveChangesAsync();
        await RecalculateRoundStatus(instance.TestRequestSubjectId);

        return await GetWorksheetInstance(id);
    }

    public async Task<Result<WorksheetInstanceDetailDto>> SaveValues(
        Guid id, SaveWorksheetValuesRequest request, Guid userId)
    {
        var instance = await context.QcWorksheetInstances.SingleOrDefaultAsync(item => item.Id == id);
        if (instance is null)
            return Result.Failure<WorksheetInstanceDetailDto>(QcWorksheetErrors.WorksheetInstanceNotFound(id));

        var assignment = EnsureAssignee(instance, userId);
        if (!assignment.IsSuccess)
            return Result.Failure<WorksheetInstanceDetailDto>(assignment.Error);

        if (instance.Status != WorksheetInstanceStatus.InProgress)
            return Result.Failure<WorksheetInstanceDetailDto>(
                QcWorksheetErrors.EnterValuesRequiresInProgress(instance.Status));

        var template = await LoadPinnedTemplate(instance.WorksheetTemplateId);
        if (template is null)
            return Result.Failure<WorksheetInstanceDetailDto>(
                QcWorksheetErrors.TemplateNotFound(instance.WorksheetTemplateId));

        var fieldsByKey = template.Sections
            .SelectMany(section => section.Fields)
            .ToDictionary(field => field.FieldKey, StringComparer.OrdinalIgnoreCase);

        var entries = request.FieldValues ?? [];

        // Nothing is persisted until every entry has been accepted, so a rejected gate leaves
        // no half-written worksheet behind.
        foreach (var entry in entries)
        {
            if (!fieldsByKey.TryGetValue(entry.FieldKey ?? string.Empty, out var field))
                return Result.Failure<WorksheetInstanceDetailDto>(
                    QcWorksheetErrors.UnknownFieldKey(entry.FieldKey));

            // The header block, and every other fixed method parameter, is rendered from the
            // template and never entered — there is no write path to it.
            if (field.Mode == WorksheetFieldMode.Constant)
                return Result.Failure<WorksheetInstanceDetailDto>(
                    QcWorksheetErrors.FieldIsNotEnterable(field.FieldKey, field.Mode));

            // A Calculated field is derived from the worksheet's own entries at submission, so
            // it has no analyst write path either. Accepting a typed value here would store a
            // number that submission then silently overwrote.
            if (field.Mode == WorksheetFieldMode.Calculated)
                return Result.Failure<WorksheetInstanceDetailDto>(
                    QcWorksheetErrors.CalculatedFieldIsNotEnterable(field.FieldKey));

            if (field.Type == WorksheetFieldType.ReferencedResult)
                return Result.Failure<WorksheetInstanceDetailDto>(
                    QcWorksheetErrors.ReferencedResultIsNotEnterable(field.FieldKey));

            if (field.Type == WorksheetFieldType.Table
                && WorksheetRowHeaders.IsHeaderColumn(field.ColumnDefinitions, entry.ColumnKey))
                return Result.Failure<WorksheetInstanceDetailDto>(
                    QcWorksheetErrors.RowHeaderIsNotEnterable(field.FieldKey, entry.ColumnKey));

            // A calculated column is computed per row at submission, exactly like a Calculated
            // field, so it has no analyst write path either.
            if (field.Type == WorksheetFieldType.Table
                && WorksheetCalculatedCells.IsCalculatedColumn(field.ColumnDefinitions, entry.ColumnKey))
                return Result.Failure<WorksheetInstanceDetailDto>(
                    QcWorksheetErrors.CalculatedColumnIsNotEnterable(field.FieldKey, entry.ColumnKey, entry.RowIndex));
        }

        var existing = await context.QcWorksheetFieldValues
            .Where(value => value.WorksheetInstanceId == id)
            .ToListAsync();

        // The gates run against what the worksheet will look like after this write, not just
        // the incoming rows: a reagent's expiry can arrive in one call and its id in another.
        var projected = Project(existing, entries, fieldsByKey, userId);
        var touched = entries
            .Select(entry => fieldsByKey[entry.FieldKey])
            .Distinct()
            .ToList();

        var touchedValues = touched
            .Select(field => new FieldWithValues(
                field,
                projected.Where(value => Matches(value, field.FieldKey)).ToList()))
            .ToList();

        // A choice must be one of the template's options (build brief 07, A1).
        foreach (var item in touchedValues)
        {
            var choice = WorksheetOptionValues.Check(item.Field, item.Values);
            if (!choice.IsSuccess)
                return Result.Failure<WorksheetInstanceDetailDto>(choice.Error);
        }

        var gate = await RunGates(touchedValues, requireComplete: false);

        if (!gate.IsSuccess)
            return Result.Failure<WorksheetInstanceDetailDto>(gate.Error);

        ApplyValues(existing, entries, id, userId);

        instance.UpdatedAt = DateTime.UtcNow;
        instance.LastUpdatedById = userId;

        // Entering results deliberately does not move the status: the worksheet stays in
        // progress until the analyst submits it.
        await context.SaveChangesAsync();

        return await GetWorksheetInstance(id);
    }

    public async Task<Result<WorksheetInstanceDetailDto>> Submit(Guid id, Guid userId)
    {
        var instance = await context.QcWorksheetInstances.SingleOrDefaultAsync(item => item.Id == id);
        if (instance is null)
            return Result.Failure<WorksheetInstanceDetailDto>(QcWorksheetErrors.WorksheetInstanceNotFound(id));

        var assignment = EnsureAssignee(instance, userId);
        if (!assignment.IsSuccess)
            return Result.Failure<WorksheetInstanceDetailDto>(assignment.Error);

        if (instance.Status != WorksheetInstanceStatus.InProgress)
            return Result.Failure<WorksheetInstanceDetailDto>(
                QcWorksheetErrors.SubmitRequiresInProgress(instance.Status));

        // QC opts out of the approval engine's silent auto-approval fallback, so a worksheet
        // cannot be submitted into a queue that has no reviewer defined — it would otherwise
        // become Reviewed with nobody having signed for it.
        var fields = await LoadFieldsWithValues(instance);

        // Defence in depth: calibration data can change between entry and submission, so the
        // gates run again here over everything recorded, not just what was last written.
        var gate = await RunGates(fields, requireComplete: true);
        if (!gate.IsSuccess)
            return Result.Failure<WorksheetInstanceDetailDto>(gate.Error);

        // Choices are re-checked over everything recorded, for the same defence-in-depth reason.
        foreach (var entry in fields)
        {
            var choice = WorksheetOptionValues.Check(entry.Field, entry.Values);
            if (!choice.IsSuccess)
                return Result.Failure<WorksheetInstanceDetailDto>(choice.Error);
        }

        foreach (var entry in fields)
        {
            if (entry.Field.Type == WorksheetFieldType.ReferencedResult)
                continue;

            if (!IsRequiredForSubmission(entry.Field))
                continue;

            // A calculated column's cells are system output, never evidence the analyst entered
            // anything — a stale result from an earlier submission must not satisfy this check.
            if (WorksheetCalculatedCells.EnteredValues(entry.Field, entry.Values)
                .All(value => string.IsNullOrWhiteSpace(value.Value)))
                return Result.Failure<WorksheetInstanceDetailDto>(
                    QcWorksheetErrors.RequiredFieldMissing(entry.Field.FieldKey, entry.Field.Label));
        }

        // Every ReferencedResult must have resolved. The resolutions are collected rather than
        // written straight away, because the calculations below may still refuse the submission
        // — and a refused submit must leave nothing behind.
        var resolutions = new List<(FieldWithValues Entry, ReferencedResultDto Resolution)>();

        foreach (var entry in fields.Where(item => item.Field.Type == WorksheetFieldType.ReferencedResult))
        {
            var resolution = await ResolveReferencedResult(instance, entry.Field, fields);

            if (!resolution.Resolved)
                return Result.Failure<WorksheetInstanceDetailDto>(
                    QcWorksheetErrors.ReferencedResultUnresolved(
                        entry.Field.FieldKey, resolution.ResolutionValue));

            resolutions.Add((entry, resolution));
        }

        // Calculated fields are evaluated against the data actually entered, and the result is
        // written down as a real WorksheetFieldValue. Recomputing for display alone would leave
        // the official record without the number a COA has to cite, and without an audit trail
        // for it. A ReferencedResult that has just resolved counts as an input, so the freshly
        // resolved values go in alongside the entered ones.
        var calculationInputs = fields.SelectMany(item => item.Values).ToList();

        calculationInputs.AddRange(resolutions.Select(item => new WorksheetFieldValue
        {
            FieldKey = item.Entry.Field.FieldKey,
            Value = item.Resolution.Value
        }));

        if (!QcWorksheetCalculator.TryEvaluateAll(
                fields.Select(item => item.Field).ToList(),
                calculationInputs,
                out var calculated,
                out var failure))
            return Result.Failure<WorksheetInstanceDetailDto>(failure.ColumnKey is null
                ? QcWorksheetErrors.CalculatedFieldUnevaluatable(
                    failure.Field.FieldKey, failure.Field.Label, failure.Reason)
                : QcWorksheetErrors.CalculatedCellUnevaluatable(
                    failure.Field.FieldKey, failure.ColumnKey, failure.RowIndex, failure.Reason));

        // Past every gate, so the system-computed values are written down now. The resolved
        // reference trace has to survive even if its source is superseded afterwards, and the
        // calculated result has to survive independently of the inputs it came from.
        foreach (var (entry, resolution) in resolutions)
            PersistSystemValue(
                entry, instance.Id, resolution.Value, resolution.ResolvedFromInstanceId, userId);

        foreach (var item in calculated.Where(item => item.ColumnKey is null))
        {
            var entry = fields.First(field =>
                string.Equals(field.Field.FieldKey, item.Field.FieldKey, StringComparison.OrdinalIgnoreCase));

            PersistSystemValue(entry, instance.Id, item.Value, resolvedFromInstanceId: null, userId);
        }

        // Per-row calculated columns: one value per (row, column), stored like an entered cell.
        foreach (var entry in fields.Where(item => item.Field.Type == WorksheetFieldType.Table))
            WorksheetCalculatedCells.Persist(
                context,
                entry.Field,
                entry.Values,
                calculated.Where(item => item.ColumnKey is not null && ReferenceEquals(item.Field, entry.Field)).ToList(),
                instance.Id,
                userId);

        await using var approvalTransaction = context.Database.IsRelational()
            ? await context.Database.BeginTransactionAsync() : null;
        instance.Status = WorksheetInstanceStatus.Submitted;
        instance.SubmittedAt = DateTime.UtcNow;
        instance.UpdatedAt = DateTime.UtcNow;
        instance.LastUpdatedById = userId;

        await context.SaveChangesAsync();

        // Puts it into the reviewer's pending-approvals queue through the same engine every
        // other module uses. No QcApproval row is signed here — that happens on review.
        await approvalRepository.CreateInitialApprovalsAsync(ModelType, id, userId);

        // Automatic OOS detection (Milestone 4): every Result field is judged against the
        // Characteristic bound to it on the round's pinned Specification version. An Action
        // limit breach opens an OosCase, which blocks the round from reaching Released; an
        // Alert breach only flags for trend review and blocks nothing.
        //
        // Deliberately after the submission has been accepted and saved, not before: the
        // result stands as submitted whatever it says, and an out-of-specification value is a
        // finding to investigate rather than a reason to refuse the analyst's entry.
        await oosDetection.DetectOnSubmitAsync(id, userId);

        await RecalculateRoundStatus(instance.TestRequestSubjectId);
        if (approvalTransaction is not null) await approvalTransaction.CommitAsync();

        return await GetWorksheetInstance(id);
    }

    // -----------------------------------------------------------------------
    // Review
    // -----------------------------------------------------------------------

    public async Task<Result<WorksheetInstanceDetailDto>> Review(
        Guid id, ReviewWorksheetInstanceRequest request, Guid userId, List<Guid> roleIds)
    {
        var instance = await context.QcWorksheetInstances.SingleOrDefaultAsync(item => item.Id == id);
        if (instance is null)
            return Result.Failure<WorksheetInstanceDetailDto>(QcWorksheetErrors.WorksheetInstanceNotFound(id));

        if (instance.Status != WorksheetInstanceStatus.Submitted)
            return Result.Failure<WorksheetInstanceDetailDto>(
                QcWorksheetErrors.ReviewRequiresSubmitted(instance.Status));

        if (!request.Approve && string.IsNullOrWhiteSpace(request.Comments))
            return Result.Failure<WorksheetInstanceDetailDto>(QcWorksheetErrors.ReviewCommentsRequired);

        // Segregation of duties, checked before any signature is taken: whoever performed the
        // work cannot sign it off, whatever their role allows.
        if (await PerformedTheWork(instance, userId))
            return Result.Failure<WorksheetInstanceDetailDto>(QcWorksheetErrors.CannotReviewOwnWork);

        // The QC re-authentication wrapper: verify the caller's own password, then call the
        // same ApproveItem/RejectItem the generic approval endpoint calls, which records the
        // QcApproval row with ReauthConfirmedAt set.
        var signed = request.Approve
            ? await signatureService.SignAndApproveAsync(
                ModelType, id, userId, roleIds, request.Password, request.Comments)
            : await signatureService.SignAndRejectAsync(
                ModelType, id, userId, roleIds, request.Password, request.Comments);

        if (!signed.IsSuccess)
            return Result.Failure<WorksheetInstanceDetailDto>(signed.Error);

        // A declined review is a return for correction, so it is logged as one — the difference
        // from the standalone action being that this cycle carries a signature.
        if (!request.Approve)
        {
            await LogCorrectionReturn(instance, userId, request.Comments, signedDecision: true);

            instance.UpdatedAt = DateTime.UtcNow;
            instance.LastUpdatedById = userId;
            await context.SaveChangesAsync();
        }

        // If this worksheet was a retest an OOS case is waiting on, reviewing it closes Phase 1
        // and hands the case to QA (Milestone 4). A no-op for every other worksheet.
        if (request.Approve)
            await oosDetection.AdvanceOnRetestReviewedAsync(id, userId);

        await RecalculateRoundStatus(instance.TestRequestSubjectId);

        // Automatic certificate generation (Milestone 5). Checked after every approved review
        // rather than by a polling job: this review may have been the last one the round was
        // waiting on. The service applies the strict-hold rule itself and withholds silently when
        // anything is still outstanding — including a single unreviewed worksheet on another
        // Subject, or a single OOS case still open — so there is nothing to decide here.
        //
        // Deliberately after the transition has committed, and deliberately unable to fail the
        // reviewer's own action: their review is a completed, re-authenticated signature, and
        // discarding it because an ancillary document could not be assembled would be the wrong
        // trade every time. The same reasoning AdvanceOnRetestReviewedAsync already applies to a
        // missing approval chain.
        if (request.Approve)
            await TryGenerateCertificate(instance.TestRequestSubjectId, userId);

        return await GetWorksheetInstance(id);
    }

    /// <summary>
    /// Asks the certificate engine whether this review completed the round. Never throws into the
    /// caller — see the note at the call site.
    /// </summary>
    private async Task TryGenerateCertificate(Guid subjectId, Guid userId)
    {
        var testRequestId = await context.QcTestRequestSubjects
            .AsNoTracking()
            .Where(item => item.Id == subjectId)
            .Select(item => item.TestRequestId)
            .SingleOrDefaultAsync();

        if (testRequestId == Guid.Empty)
            return;

        await coaGeneration.TryGenerateAsync(testRequestId, userId);
    }

    public async Task<Result<WorksheetInstanceDetailDto>> ReturnForCorrection(
        Guid id, ReturnWorksheetForCorrectionRequest request, Guid userId)
    {
        var instance = await context.QcWorksheetInstances.SingleOrDefaultAsync(item => item.Id == id);
        if (instance is null)
            return Result.Failure<WorksheetInstanceDetailDto>(QcWorksheetErrors.WorksheetInstanceNotFound(id));

        if (instance.Status != WorksheetInstanceStatus.Submitted)
            return Result.Failure<WorksheetInstanceDetailDto>(
                QcWorksheetErrors.ReturnForCorrectionRequiresSubmitted(instance.Status));

        if (string.IsNullOrWhiteSpace(request.Reason))
            return Result.Failure<WorksheetInstanceDetailDto>(QcWorksheetErrors.ReviewCommentsRequired);

        await LogCorrectionReturn(instance, userId, request.Reason, signedDecision: false);

        instance.Status = WorksheetInstanceStatus.InProgress;
        instance.Approved = false;
        instance.SubmittedAt = null;
        instance.UpdatedAt = DateTime.UtcNow;
        instance.LastUpdatedById = userId;

        // The original assignee keeps the work and keeps their entered values; only a separate
        // reassignment moves it to someone else.
        await context.SaveChangesAsync();
        await RecalculateRoundStatus(instance.TestRequestSubjectId);

        return await GetWorksheetInstance(id);
    }

    /// <summary>
    /// Records one correction cycle. A row per cycle, never an overwrite: a worksheet can go
    /// round the loop more than once and each return is part of the record.
    /// </summary>
    private async Task LogCorrectionReturn(
        WorksheetInstance instance, Guid userId, string reason, bool signedDecision)
    {
        // The review round this return interrupted, so the log can be read against the
        // signature trail. Zero when the worksheet has no approval round yet.
        var approvalRound = await context.QcApprovals
            .Where(item => item.EntityType == QcApprovalEntityTypes.WorksheetInstance
                && item.EntityId == instance.Id)
            .Select(item => (int?)item.ApprovalRound)
            .MaxAsync() ?? 0;

        context.QcWorksheetInstanceCorrectionReturns.Add(new WorksheetInstanceCorrectionReturn
        {
            Id = Guid.NewGuid(),
            WorksheetInstanceId = instance.Id,
            ReturnedById = userId,
            ReturnedAt = DateTime.UtcNow,
            Reason = reason?.Trim(),
            ApprovalRound = approvalRound,
            Signed = signedDecision,
            CreatedAt = DateTime.UtcNow,
            CreatedById = userId
        });
    }

    // -----------------------------------------------------------------------
    // Assignment enforcement and segregation of duties
    // -----------------------------------------------------------------------

    /// <summary>
    /// Whether this user performed the work, and therefore may not review it.
    /// <para>
    /// Three ways to have performed it, all of which count: being the current assignee, having
    /// entered any value on the worksheet, and having held it earlier — a previous assignee is
    /// recoverable from the reassignment log, so handing a worksheet on does not launder the
    /// reviewer's independence.
    /// </para>
    /// <para>
    /// Enforced here rather than left to approval-chain configuration on purpose: a chain that
    /// happens to list the analyst as an approver would otherwise let them sign their own work,
    /// and the second pair of eyes is the entire control.
    /// </para>
    /// </summary>
    private async Task<bool> PerformedTheWork(WorksheetInstance instance, Guid userId)
    {
        if (instance.AssignedToId == userId)
            return true;

        if (await context.QcWorksheetFieldValues
                .AnyAsync(value => value.WorksheetInstanceId == instance.Id
                    && value.EnteredById == userId))
            return true;

        return await context.QcWorksheetInstanceReassignments
            .AnyAsync(item => item.WorksheetInstanceId == instance.Id
                && (item.FromUserId == userId || item.ToUserId == userId));
    }

    /// <summary>
    /// The server-side half of assignment enforcement. Holding
    /// <c>CanStartMicrobialWorksheet</c> gets a caller past the permission layer; being the
    /// assignee is what gets them past this.
    /// </summary>
    private static Result EnsureAssignee(WorksheetInstance instance, Guid userId)
    {
        if (!instance.AssignedToId.HasValue)
            return QcWorksheetErrors.NotAssigned;

        return instance.AssignedToId.Value != userId
            ? QcWorksheetErrors.NotTheAssignee
            : Result.Success();
    }

    // -----------------------------------------------------------------------
    // Hard instrument / reagent gates
    // -----------------------------------------------------------------------

    /// <summary>
    /// Blocks an expired instrument calibration or an expired reagent lot outright.
    /// <para>
    /// <paramref name="requireComplete"/> is set at submission, where a reagent entry must
    /// additionally be whole: a reagent recorded without its batch number or expiry date cannot
    /// be gated at all, which is the failure this exists to prevent.
    /// </para>
    /// </summary>
    private async Task<Result> RunGates(IReadOnlyCollection<FieldWithValues> fields, bool requireComplete)
    {
        var today = DateTime.UtcNow.Date;

        foreach (var entry in fields)
        {
            switch (entry.Field.Type)
            {
                case WorksheetFieldType.Instrument:
                {
                    foreach (var value in entry.Values.Where(item => !string.IsNullOrWhiteSpace(item.Value)))
                    {
                        var gate = await GateInstrument(entry.Field.FieldKey, value.Value, today);
                        if (!gate.IsSuccess) return gate;
                    }

                    break;
                }

                case WorksheetFieldType.Reagent:
                case WorksheetFieldType.ReferenceStandard:
                {
                    // One entry is three rows sharing a FieldKey, so they are gated per
                    // (row, field) group rather than row by row.
                    var groups = entry.Values
                        .GroupBy(value => value.RowIndex)
                        .Where(group => group.Any(value => !string.IsNullOrWhiteSpace(value.Value)));

                    foreach (var group in groups)
                    {
                        var gate = await GateReagent(entry.Field.FieldKey, group.ToList(), today, requireComplete);
                        if (!gate.IsSuccess) return gate;
                    }

                    break;
                }
            }
        }

        return Result.Success();
    }

    private async Task<Result> GateInstrument(string fieldKey, string value, DateTime today)
    {
        if (!Guid.TryParse(value?.Trim(), out var equipmentId))
            return QcWorksheetErrors.InstrumentNotFound(fieldKey, value);

        // The existing equipment register, read-only. This milestone never writes to it.
        var equipment = await context.QcEquipments
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == equipmentId);

        if (equipment is null)
            return QcWorksheetErrors.InstrumentNotFound(fieldKey, value);

        var name = string.IsNullOrWhiteSpace(equipment.Name) ? equipment.EquipmentId : equipment.Name;

        // Unknown calibration is treated as failing calibration: equipment that cannot
        // demonstrate it is in date is not equipment this test may rely on.
        if (!equipment.CalibrationDueDate.HasValue)
            return QcWorksheetErrors.InstrumentCalibrationUnknown(fieldKey, name);

        return equipment.CalibrationDueDate.Value.Date < today
            ? QcWorksheetErrors.InstrumentCalibrationExpired(fieldKey, name, equipment.CalibrationDueDate.Value)
            : Result.Success();
    }

    private async Task<Result> GateReagent(
        string fieldKey, IReadOnlyCollection<WorksheetFieldValue> group, DateTime today, bool requireComplete)
    {
        string SubValue(string columnKey) => group
            .FirstOrDefault(value => string.Equals(value.ColumnKey, columnKey, StringComparison.OrdinalIgnoreCase))
            ?.Value;

        var reagentId = SubValue(QcWorksheetValueColumns.ReagentId);
        var batchNo = SubValue(QcWorksheetValueColumns.BatchNo);
        var expiry = SubValue(QcWorksheetValueColumns.ExpiryDate);

        if (requireComplete
            && (string.IsNullOrWhiteSpace(reagentId)
                || string.IsNullOrWhiteSpace(batchNo)
                || string.IsNullOrWhiteSpace(expiry)))
            return QcWorksheetErrors.ReagentEntryIncomplete(fieldKey);

        if (!string.IsNullOrWhiteSpace(reagentId))
        {
            // The catalog lookup is the only part of a reagent entry that is master data; the
            // batch and expiry are captured fresh because nothing tracks them.
            if (!Guid.TryParse(reagentId.Trim(), out var id)
                || !await context.Reagents.AnyAsync(item => item.Id == id))
                return QcWorksheetErrors.ReagentNotFound(fieldKey, reagentId);
        }

        if (string.IsNullOrWhiteSpace(expiry))
            return Result.Success();

        if (!TryParseDate(expiry, out var expiryDate))
            return QcWorksheetErrors.ReagentExpiryUnreadable(fieldKey, expiry);

        return expiryDate.Date < today
            ? QcWorksheetErrors.ReagentExpired(fieldKey, expiryDate)
            : Result.Success();
    }

    private static bool TryParseDate(string value, out DateTime parsed) =>
        DateTime.TryParse(
            value?.Trim(),
            CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
            out parsed);

    // -----------------------------------------------------------------------
    // ReferencedResult runtime resolution
    // -----------------------------------------------------------------------

    /// <summary>
    /// Resolves a ReferencedResult field against another template's instances at read time.
    /// <para>
    /// Only a <b>Reviewed</b> source is trusted (Locked counts too — it is Reviewed plus an
    /// issued certificate). A merely submitted result is somebody's unchecked entry, and
    /// pulling it into a second worksheet would launder it into an approved-looking value.
    /// </para>
    /// <para>
    /// An unresolved field is pending, not broken: the analyst usually just has not entered the
    /// lookup key yet. It blocks submission, never the rest of the worksheet.
    /// </para>
    /// </summary>
    private async Task<ReferencedResultDto> ResolveReferencedResult(
        WorksheetInstance instance, WorksheetField field, IReadOnlyCollection<FieldWithValues> fields)
    {
        var resolution = new ReferencedResultDto
        {
            SourceTemplateId = field.ReferencedResultSourceTemplateId,
            SourceFieldKey = field.ReferencedResultSourceFieldKey,
            ResolutionFieldKey = field.ReferencedResultResolutionFieldKey,
            Resolved = false
        };

        if (!field.ReferencedResultSourceTemplateId.HasValue
            || string.IsNullOrWhiteSpace(field.ReferencedResultSourceFieldKey)
            || string.IsNullOrWhiteSpace(field.ReferencedResultResolutionFieldKey))
        {
            resolution.Message = "This referenced result is not fully configured on the worksheet template.";
            return resolution;
        }

        resolution.SourceTemplateCode = await context.QcWorksheetTemplates
            .AsNoTracking()
            .Where(template => template.Id == field.ReferencedResultSourceTemplateId.Value)
            .Select(template => template.Code)
            .SingleOrDefaultAsync();

        // Step 1: the lookup key, read from this instance. Typically the paired Reagent field's
        // batch number, falling back to a plain scalar value on that same field.
        var source = fields.FirstOrDefault(item => string.Equals(
            item.Field.FieldKey, field.ReferencedResultResolutionFieldKey, StringComparison.OrdinalIgnoreCase));

        var resolutionValue = source?.Values
            .Where(value => string.Equals(
                value.ColumnKey, QcWorksheetValueColumns.BatchNo, StringComparison.OrdinalIgnoreCase))
            .Select(value => value.Value)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))
            ?? source?.Values
                .Where(value => value.ColumnKey is null)
                .Select(value => value.Value)
                .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

        if (string.IsNullOrWhiteSpace(resolutionValue))
        {
            resolution.Message =
                $"Enter '{field.ReferencedResultResolutionFieldKey}' first — this value resolves from it.";
            return resolution;
        }

        resolution.ResolutionValue = resolutionValue.Trim();

        // Step 2: the most recent reviewed instance of the source template whose own batch
        // number matches.
        var candidates = await context.QcWorksheetInstances
            .AsNoTracking()
            .Where(item => item.WorksheetTemplateId == field.ReferencedResultSourceTemplateId.Value
                && item.Id != instance.Id
                && (item.Status == WorksheetInstanceStatus.Reviewed
                    || item.Status == WorksheetInstanceStatus.Locked))
            .OrderByDescending(item => item.SubmittedAt)
            .ThenByDescending(item => item.CreatedAt)
            .Select(item => item.Id)
            .ToListAsync();

        if (candidates.Count == 0)
        {
            resolution.Message =
                $"No matching qualification found for batch {resolution.ResolutionValue}.";
            return resolution;
        }

        var values = await context.QcWorksheetFieldValues
            .AsNoTracking()
            .Where(value => candidates.Contains(value.WorksheetInstanceId))
            .ToListAsync();

        foreach (var candidateId in candidates)
        {
            var candidateValues = values.Where(value => value.WorksheetInstanceId == candidateId).ToList();

            var matches = candidateValues.Any(value =>
                string.Equals(value.ColumnKey, QcWorksheetValueColumns.BatchNo, StringComparison.OrdinalIgnoreCase)
                && string.Equals(value.Value?.Trim(), resolution.ResolutionValue, StringComparison.OrdinalIgnoreCase))
                || candidateValues.Any(value =>
                    value.ColumnKey is null
                    && string.Equals(
                        value.FieldKey, field.ReferencedResultResolutionFieldKey, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(
                        value.Value?.Trim(), resolution.ResolutionValue, StringComparison.OrdinalIgnoreCase));

            if (!matches)
                continue;

            var sourceValue = candidateValues.FirstOrDefault(value =>
                string.Equals(value.FieldKey, field.ReferencedResultSourceFieldKey, StringComparison.OrdinalIgnoreCase)
                && value.ColumnKey is null);

            if (sourceValue is null || string.IsNullOrWhiteSpace(sourceValue.Value))
            {
                resolution.Message =
                    $"The matching worksheet has no value for '{field.ReferencedResultSourceFieldKey}'.";
                return resolution;
            }

            resolution.Resolved = true;
            resolution.ResolvedFromInstanceId = candidateId;
            resolution.Value = sourceValue.Value;
            return resolution;
        }

        resolution.Message = $"No matching qualification found for batch {resolution.ResolutionValue}.";
        return resolution;
    }

    /// <summary>
    /// Writes a system-computed scalar down at submission — a resolved ReferencedResult (with
    /// the source instance recorded on it, so the ARD reconstructs without re-running the
    /// lookup) or an evaluated Calculated field.
    /// <para>
    /// Both are stored as ordinary <see cref="WorksheetFieldValue"/> rows rather than in a
    /// parallel shape: the official record should not distinguish between a number a person
    /// typed and one the system derived, beyond the attribution already carried on every row.
    /// Re-submitting after a correction overwrites in place, so the stored result always matches
    /// the inputs standing at the last submission.
    /// </para>
    /// </summary>
    private void PersistSystemValue(
        FieldWithValues entry,
        Guid instanceId,
        string value,
        Guid? resolvedFromInstanceId,
        Guid userId)
    {
        var existing = entry.Values.FirstOrDefault(item => item.ColumnKey is null && item.RowIndex is null);

        if (existing is null)
        {
            context.QcWorksheetFieldValues.Add(new WorksheetFieldValue
            {
                Id = Guid.NewGuid(),
                WorksheetInstanceId = instanceId,
                FieldKey = entry.Field.FieldKey,
                Value = value,
                EnteredById = userId,
                EnteredAt = DateTime.UtcNow,
                ResolvedFromInstanceId = resolvedFromInstanceId,
                CreatedAt = DateTime.UtcNow,
                CreatedById = userId
            });

            return;
        }

        // Already tracked — LoadFieldsWithValues reads these for update, not no-tracking — so
        // mutating is enough and the save that follows picks it up.
        existing.Value = value;
        existing.ResolvedFromInstanceId = resolvedFromInstanceId;
        existing.EnteredById = userId;
        existing.EnteredAt = DateTime.UtcNow;
        existing.UpdatedAt = DateTime.UtcNow;
        existing.LastUpdatedById = userId;
    }

    // -----------------------------------------------------------------------
    // Value persistence
    // -----------------------------------------------------------------------

    /// <summary>
    /// What the stored values will look like after this write, without writing anything — the
    /// gates need the merged picture, not just the incoming rows.
    /// </summary>
    private static List<WorksheetFieldValue> Project(
        IReadOnlyCollection<WorksheetFieldValue> existing,
        IReadOnlyCollection<WorksheetFieldValueEntry> entries,
        IReadOnlyDictionary<string, WorksheetField> fieldsByKey,
        Guid userId)
    {
        var projected = existing
            .Select(value => new WorksheetFieldValue
            {
                Id = value.Id,
                WorksheetInstanceId = value.WorksheetInstanceId,
                FieldKey = value.FieldKey,
                RowIndex = value.RowIndex,
                ColumnKey = value.ColumnKey,
                Value = value.Value
            })
            .ToList();

        foreach (var entry in entries)
        {
            var key = fieldsByKey[entry.FieldKey].FieldKey;
            var match = projected.FirstOrDefault(value => IsSameCell(value, key, entry));

            if (match is null)
            {
                projected.Add(new WorksheetFieldValue
                {
                    FieldKey = key,
                    RowIndex = entry.RowIndex,
                    ColumnKey = entry.ColumnKey,
                    Value = entry.Value,
                    EnteredById = userId
                });

                continue;
            }

            match.Value = entry.Value;
        }

        return projected;
    }

    private void ApplyValues(
        List<WorksheetFieldValue> existing,
        IReadOnlyCollection<WorksheetFieldValueEntry> entries,
        Guid instanceId,
        Guid userId)
    {
        foreach (var entry in entries)
        {
            var match = existing.FirstOrDefault(value => IsSameCell(value, entry.FieldKey, entry));

            // An emptied cell is removed rather than stored as a blank, so "has a value" stays
            // a single, unambiguous question at submission.
            if (string.IsNullOrWhiteSpace(entry.Value))
            {
                if (match is not null)
                {
                    context.QcWorksheetFieldValues.Remove(match);
                    existing.Remove(match);
                }

                continue;
            }

            if (match is null)
            {
                var added = new WorksheetFieldValue
                {
                    Id = Guid.NewGuid(),
                    WorksheetInstanceId = instanceId,
                    FieldKey = entry.FieldKey,
                    RowIndex = entry.RowIndex,
                    ColumnKey = entry.ColumnKey,
                    Value = entry.Value,
                    EnteredById = userId,
                    EnteredAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    CreatedById = userId
                };

                context.QcWorksheetFieldValues.Add(added);
                existing.Add(added);
                continue;
            }

            // Attribution moves to whoever is entering now — which, because of assignment
            // enforcement, is always the person actually permitted to be doing the work.
            match.Value = entry.Value;
            match.EnteredById = userId;
            match.EnteredAt = DateTime.UtcNow;
            match.UpdatedAt = DateTime.UtcNow;
            match.LastUpdatedById = userId;
        }
    }

    private static bool IsSameCell(WorksheetFieldValue value, string fieldKey, WorksheetFieldValueEntry entry) =>
        string.Equals(value.FieldKey, fieldKey, StringComparison.OrdinalIgnoreCase)
        && value.RowIndex == entry.RowIndex
        && string.Equals(value.ColumnKey ?? string.Empty, entry.ColumnKey ?? string.Empty,
            StringComparison.OrdinalIgnoreCase);

    private static bool Matches(WorksheetFieldValue value, string fieldKey) =>
        string.Equals(value.FieldKey, fieldKey, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Which fields <c>/submit</c> insists on. The worksheet template carries no per-field
    /// "required" flag, so the rule is structural: every Entry-mode field that captures data
    /// must have one. Headings and instructions capture nothing, and Constant/Calculated fields
    /// are not the analyst's to fill.
    /// </summary>
    private static bool IsRequiredForSubmission(WorksheetField field) =>
        field.Mode == WorksheetFieldMode.Entry
        && field.Type is not (WorksheetFieldType.Instructions or WorksheetFieldType.Heading);

    // -----------------------------------------------------------------------
    // Loading
    // -----------------------------------------------------------------------

    /// <summary>
    /// Loads the <b>pinned</b> template version — the row this instance's
    /// <c>WorksheetTemplateId</c> names, and nothing else. There is deliberately no walk
    /// forward through <c>SupersedesId</c>: an in-flight worksheet never upgrades, not even for
    /// a non-breaking revision.
    /// </summary>
    private async Task<WorksheetTemplate> LoadPinnedTemplate(Guid templateId) =>
        await context.QcWorksheetTemplates
            .AsNoTracking()
            .Include(item => item.Stp)
            .Include(item => item.Sections.OrderBy(section => section.Order))
                .ThenInclude(section => section.Fields.OrderBy(field => field.Order))
            .AsSplitQuery()
            .SingleOrDefaultAsync(item => item.Id == templateId);

    private async Task<List<FieldWithValues>> LoadFieldsWithValues(WorksheetInstance instance)
    {
        var template = await LoadPinnedTemplate(instance.WorksheetTemplateId);
        if (template is null)
            return [];

        var values = await context.QcWorksheetFieldValues
            .Where(value => value.WorksheetInstanceId == instance.Id)
            .ToListAsync();

        return template.Sections
            .SelectMany(section => section.Fields)
            .Select(field => new FieldWithValues(
                field,
                values.Where(value => Matches(value, field.FieldKey)).ToList()))
            .ToList();
    }

    private async Task<WorksheetInstance> LoadDetail(Guid id) =>
        await context.QcWorksheetInstances
            .AsNoTracking()
            .Include(item => item.CreatedBy)
            .Include(item => item.AssignedTo)
            .Include(item => item.WorksheetTemplate)
            .Include(item => item.TestRequestSubject)
                .ThenInclude(subject => subject.TestRequest)
                    .ThenInclude(request => request.Specification)
            .Include(item => item.TestRequestSubject)
                .ThenInclude(subject => subject.TestRequest)
                    .ThenInclude(request => request.IssuedBy)
            .Include(item => item.TestRequestSubject)
                .ThenInclude(subject => subject.MaterialBatch)
            .Include(item => item.TestRequestSubject)
                .ThenInclude(subject => subject.BatchManufacturingRecord)
            .AsSplitQuery()
            .SingleOrDefaultAsync(item => item.Id == id);

    private IQueryable<WorksheetInstance> QueueQuery() =>
        context.QcWorksheetInstances
            .AsNoTracking()
            .Include(item => item.AssignedTo)
            .Include(item => item.WorksheetTemplate)
            .Include(item => item.TestRequestSubject)
                .ThenInclude(subject => subject.TestRequest)
                    .ThenInclude(request => request.Specification)
            .AsSplitQuery()
            .OrderBy(item => item.CreatedAt);

    private async Task<bool> HasConfiguredApprovalChain()
    {
        var approval = await context.Approvals.FirstOrDefaultAsync(item => item.ItemType == ModelType);
        if (approval is null)
            return false;

        return await context.ApprovalStages.AnyAsync(stage => stage.ApprovalId == approval.Id);
    }

    private async Task RecalculateRoundStatus(Guid subjectId)
    {
        var testRequestId = await context.QcTestRequestSubjects
            .AsNoTracking()
            .Where(subject => subject.Id == subjectId)
            .Select(subject => subject.TestRequestId)
            .SingleOrDefaultAsync();

        if (testRequestId != Guid.Empty)
            await QcTestRequestStatusCalculator.RecalculateAsync(context, testRequestId);
    }

    // -----------------------------------------------------------------------
    // Projection
    // -----------------------------------------------------------------------

    private WorksheetQueueItemDto ToQueueItem(
        WorksheetInstance instance, Dictionary<Guid, Guid> openOosCases)
    {
        var dto = QcWorksheetInstanceMapper.Fill(new WorksheetQueueItemDto(), instance, mapper);
        var subject = instance.TestRequestSubject;
        var request = subject?.TestRequest;

        // The reviewer queue's red flag: this result is already under formal OOS investigation.
        if (openOosCases.TryGetValue(instance.Id, out var oosCaseId))
        {
            dto.HasOpenOosCase = true;
            dto.OosCaseId = oosCaseId;
        }

        dto.TestRequestId = request?.Id ?? Guid.Empty;
        dto.TestRequestType = request?.Type ?? default;
        dto.TestRequestArNumber = request?.ArNumber;
        dto.SubjectRef = subject?.SubjectRef;
        dto.SubjectLabel = subject?.SubjectLabel;
        dto.SubjectArNumber = subject?.ArNumber;
        dto.CollectedAt = subject?.CollectedAt;
        dto.SpecificationCode = request?.Specification?.Code;

        // The round's pin, as stored.
        dto.SpecificationVersion = request?.SpecificationVersion ?? 0;
        return dto;
    }

    private async Task<WorksheetInstanceDetailDto> ToDetailDto(WorksheetInstance instance)
    {
        var dto = QcWorksheetInstanceMapper.Fill(new WorksheetInstanceDetailDto(), instance, mapper);

        var template = await LoadPinnedTemplate(instance.WorksheetTemplateId);

        var values = await context.QcWorksheetFieldValues
            .AsNoTracking()
            .Include(value => value.EnteredBy)
            .Where(value => value.WorksheetInstanceId == instance.Id)
            .ToListAsync();

        var reassignments = await context.QcWorksheetInstanceReassignments
            .AsNoTracking()
            .Include(item => item.FromUser)
            .Include(item => item.ToUser)
            .Include(item => item.ReassignedBy)
            .Where(item => item.WorksheetInstanceId == instance.Id)
            .OrderBy(item => item.ReassignedAt)
            .ToListAsync();

        var correctionReturns = await context.QcWorksheetInstanceCorrectionReturns
            .AsNoTracking()
            .Include(item => item.ReturnedBy)
            .Where(item => item.WorksheetInstanceId == instance.Id)
            .OrderBy(item => item.ReturnedAt)
            .ToListAsync();

        // The worksheet's own OOS state, straight off the OosCase rows — no joins, because
        // everything projected here lives on the case itself. A reviewer needs the backend's
        // real answer here rather than re-deriving one from acceptance-criteria text, which can
        // disagree with the LimitEvaluator grammar that actually opened (or did not open) these
        // cases.
        dto.OosCases = await context.QcOosCases
            .AsNoTracking()
            .Where(item => item.WorksheetInstanceId == instance.Id)
            .OrderBy(item => item.OpenedAt)
            .Select(item => new WorksheetInstanceOosCaseDto
            {
                Id = item.Id,
                FieldKey = item.FieldKey,
                Status = item.Status,
                OpenedAt = item.OpenedAt,
                ObservedValue = item.ObservedValue,
                BreachedLimit = item.BreachedLimit,
                DispositionOutcome = item.DispositionOutcome,
                BlocksRelease = item.Status != OosCaseStatus.Closed,
                RetestWorksheetInstanceId = item.RetestWorksheetInstanceId
            })
            .ToListAsync();

        dto.Header = BuildHeader(instance, template, reassignments);

        dto.CorrectionReturns = correctionReturns
            .Select(item => new WorksheetInstanceCorrectionReturnDto
            {
                Id = item.Id,
                WorksheetInstanceId = item.WorksheetInstanceId,
                ReturnedById = item.ReturnedById,
                ReturnedBy = item.ReturnedBy is null ? null : mapper.Map<UserDto>(item.ReturnedBy),
                ReturnedAt = item.ReturnedAt,
                Reason = item.Reason,
                ApprovalRound = item.ApprovalRound,
                Signed = item.Signed,
                CreatedAt = item.CreatedAt
            })
            .ToList();

        dto.Reassignments = reassignments
            .Select(item => new WorksheetInstanceReassignmentDto
            {
                Id = item.Id,
                WorksheetInstanceId = item.WorksheetInstanceId,
                FromUserId = item.FromUserId,
                FromUser = item.FromUser is null ? null : mapper.Map<UserDto>(item.FromUser),
                ToUserId = item.ToUserId,
                ToUser = item.ToUser is null ? null : mapper.Map<UserDto>(item.ToUser),
                ReassignedById = item.ReassignedById,
                ReassignedBy = item.ReassignedBy is null ? null : mapper.Map<UserDto>(item.ReassignedBy),
                ReassignedAt = item.ReassignedAt,
                Reason = item.Reason,
                CreatedAt = item.CreatedAt
            })
            .ToList();

        if (template is null)
            return dto;

        var fields = template.Sections
            .SelectMany(section => section.Fields)
            .Select(field => new FieldWithValues(
                field,
                values.Where(value => Matches(value, field.FieldKey)).ToList()))
            .ToList();

        foreach (var section in template.Sections.OrderBy(section => section.Order))
        {
            var sectionDto = new WorksheetInstanceSectionDto
            {
                Id = section.Id,
                Order = section.Order,
                Name = section.Name,
                InstrumentId = section.InstrumentId
            };

            foreach (var field in section.Fields.OrderBy(field => field.Order))
            {
                var fieldValues = values.Where(value => Matches(value, field.FieldKey)).ToList();

                var fieldDto = new WorksheetInstanceFieldDto
                {
                    Id = field.Id,
                    Order = field.Order,
                    FieldKey = field.FieldKey,
                    Label = field.Label,
                    Type = field.Type,
                    Mode = field.Mode,
                    Unit = field.Unit,
                    Analyte = field.Analyte,
                    ConstantValue = field.ConstantValue,
                    FormulaExpression = field.FormulaExpression,
                    ColumnDefinitions = field.ColumnDefinitions,
                    OptionsJson = field.OptionsJson,
                    ReadOnly =field.Mode != WorksheetFieldMode.Entry
                        || field.Type == WorksheetFieldType.ReferencedResult,
                    RequiredForSubmission = field.Type == WorksheetFieldType.ReferencedResult
                        || IsRequiredForSubmission(field),
                    Values = fieldValues
                        .OrderBy(value => value.RowIndex)
                        .ThenBy(value => value.ColumnKey)
                        .Select(value => new WorksheetFieldValueDto
                        {
                            Id = value.Id,
                            FieldKey = value.FieldKey,
                            RowIndex = value.RowIndex,
                            ColumnKey = value.ColumnKey,
                            Value = value.Value,
                            EnteredById = value.EnteredById,
                            EnteredBy = value.EnteredBy is null ? null : mapper.Map<UserDto>(value.EnteredBy),
                            EnteredAt = value.EnteredAt,
                            ResolvedFromInstanceId = value.ResolvedFromInstanceId,
                            CreatedAt = value.CreatedAt
                        })
                        .ToList()
                };

                if (field.Type == WorksheetFieldType.ReferencedResult)
                    fieldDto.ReferencedResult = await ResolveReferencedResult(instance, field, fields);

                sectionDto.Fields.Add(fieldDto);
            }

            dto.Sections.Add(sectionDto);
        }

        return dto;
    }

    /// <summary>
    /// The fixed header every real filled ARD prints, computed on every read and stored
    /// nowhere. The Specification and STP references resolve through the <b>pinned</b> rows, so
    /// a reprinted ARD names the documents the test actually ran under.
    /// </summary>
    private WorksheetInstanceHeaderDto BuildHeader(
        WorksheetInstance instance,
        WorksheetTemplate template,
        IReadOnlyCollection<WorksheetInstanceReassignment> reassignments)
    {
        var subject = instance.TestRequestSubject;
        var request = subject?.TestRequest;
        var specification = request?.Specification;

        // The window opens when the test first entered someone's hands: a reassignment
        // overwrites AssignedAt, so the audit trail is what keeps the real start visible.
        var firstReassignment = reassignments.Count == 0
            ? (DateTime?)null
            : reassignments.Min(item => item.ReassignedAt);

        var analysisFrom = instance.AssignedAt.HasValue && firstReassignment.HasValue
            ? (instance.AssignedAt.Value < firstReassignment.Value ? instance.AssignedAt : firstReassignment)
            : instance.AssignedAt ?? firstReassignment;

        return new WorksheetInstanceHeaderDto
        {
            TestRequestId = request?.Id ?? Guid.Empty,
            TestRequestType = request?.Type ?? default,
            SubjectRef = subject?.SubjectRef,
            SubjectLabel = subject?.SubjectLabel,

            // The Subject's own AR sub-number when it has one, otherwise the round's.
            ArNumber = string.IsNullOrWhiteSpace(subject?.ArNumber) ? request?.ArNumber : subject.ArNumber,
            SpecificationCode = specification?.Code,

            // The round's pin, never the specification row's current version.
            SpecificationVersion = request?.SpecificationVersion ?? 0,
            SpecificationRevision = request is null ? null : $"Rev {request.SpecificationVersion}",

            // Resolved through the pinned template version's own StpId.
            StpCode = template?.Stp?.Code,
            StpId = template?.StpId,
            WorksheetTemplateCode = template?.Code,
            WorksheetTemplateName = template?.Name,
            WorksheetTemplateVersion = instance.WorksheetTemplateVersion,
            IssueNumber = request?.IssueNumber,
            IssuedAt = request?.IssuedAt,
            IssuedBy = request?.IssuedBy is null ? null : mapper.Map<UserDto>(request.IssuedBy),

            // Material/Product only, read from the linked batch record; blank for Water/EM,
            // which have no manufacturing or expiry date to print.
            ManufacturingDate = subject?.MaterialBatch?.ManufacturingDate
                ?? subject?.BatchManufacturingRecord?.ManufacturingDate,
            ExpiryDate = subject?.MaterialBatch?.ExpiryDate
                ?? subject?.BatchManufacturingRecord?.ExpiryDate,
            SampledOn = subject?.CollectedAt,
            AnalysisDateFrom = analysisFrom,
            AnalysisDateTo = instance.SubmittedAt
        };
    }
}
