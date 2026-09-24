using APP.IRepository;
using APP.Services.QcWorksheets;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.QcWorksheets;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository.QcWorksheets;

/// <summary>
/// The formal OOS/OOT investigation: Phase 1 lab-error check, retest authorization or
/// escalation, and the QA disposition that finally decides a batch's fate.
/// <para>
/// Four rules govern everything here.
/// </para>
/// <para>
/// <b>The original result is never touched.</b> A retest is a new
/// <see cref="WorksheetInstance"/> linked by <c>RetestOfInstanceId</c>, not the original
/// reopened. Nothing in this repository writes a <c>WorksheetFieldValue</c>, and nothing
/// deletes or supersedes the instance that triggered the case — both results stay visible in
/// the trace, and the disposition records which one counts.
/// </para>
/// <para>
/// <b>Hard version pinning.</b> The retest runs the same
/// <c>WorksheetTemplateId</c> + <c>WorksheetTemplateVersion</c> the original was pinned to,
/// copied straight off the original instance rather than re-read from whichever template
/// version is Effective now. The Specification is loaded by the round's pinned id. Nothing here
/// walks <c>SupersedesId</c>.
/// </para>
/// <para>
/// <b>Quarantine is a human act.</b> Auto-creation opens the case and blocks release; it does
/// not touch a batch. The real batch status only moves when someone starts the investigation,
/// because locking a batch out of use across Warehouse and Production deserves a moment's
/// confirmation first.
/// </para>
/// <para>
/// <b>One signature mechanism.</b> The disposition goes through the shared
/// <see cref="QcApproval"/> table and the Milestone 1 re-authentication wrapper, exactly like
/// every other QC approval point. There is no OOS-only e-signature path.
/// </para>
/// </summary>
public class OosCaseRepository(
    ApplicationDbContext context,
    IMapper mapper,
    IQcSignatureService signatureService,
    IApprovalRepository approvalRepository) : IOosCaseRepository
{
    private const string ModelType = QcWorksheetModelTypes.OosCase;

    // -----------------------------------------------------------------------
    // Reads
    // -----------------------------------------------------------------------

    public async Task<Result<Paginateable<IEnumerable<OosCaseSummaryDto>>>> GetOosCases(
        int page, int pageSize, string searchQuery, OosCaseStatus? status)
    {
        var query = context.QcOosCases.AsNoTracking().AsQueryable();

        if (status.HasValue)
            query = query.Where(item => item.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var term = searchQuery.Trim().ToLower();
            query = query.Where(item =>
                item.FieldKey.ToLower().Contains(term)
                || item.WorksheetInstance.TestRequestSubject.SubjectRef.ToLower().Contains(term)
                || item.WorksheetInstance.TestRequestSubject.TestRequest.ArNumber.ToLower().Contains(term));
        }

        var ordered = query
            .Include(item => item.SpecificationCharacteristic)
            .Include(item => item.WorksheetInstance)
                .ThenInclude(instance => instance.WorksheetTemplate)
            .Include(item => item.WorksheetInstance)
                .ThenInclude(instance => instance.TestRequestSubject)
                    .ThenInclude(subject => subject.TestRequest)
            .AsSplitQuery()
            .OrderByDescending(item => item.OpenedAt);

        return await PaginationHelper.GetPaginatedResultAsync(ordered, page, pageSize, ToSummaryDto);
    }

    public async Task<Result<OosCaseDetailDto>> GetOosCase(Guid id)
    {
        var oosCase = await LoadDetail(id);
        return oosCase is null
            ? Result.Failure<OosCaseDetailDto>(QcWorksheetErrors.OosCaseNotFound(id))
            : Result.Success(await ToDetailDto(oosCase));
    }

    /// <summary>
    /// Whether a round is held by an unclosed OOS case. This is the single source of truth for
    /// the release block — Milestone 5's certificate flow asks this rather than re-deriving it.
    /// </summary>
    public async Task<Result<bool>> IsReleaseBlocked(Guid testRequestId)
    {
        var openCases = await CountOpenCases(testRequestId);
        return Result.Success(openCases > 0);
    }

    // -----------------------------------------------------------------------
    // Phase 1 investigation
    // -----------------------------------------------------------------------

    /// <summary>
    /// Starts the Phase 1 lab-error check — and this, not auto-creation, is where the real
    /// batch quarantine happens.
    /// </summary>
    public async Task<Result<OosCaseDetailDto>> StartInvestigation(Guid id, Guid userId)
    {
        var oosCase = await context.QcOosCases.SingleOrDefaultAsync(item => item.Id == id);
        if (oosCase is null)
            return Result.Failure<OosCaseDetailDto>(QcWorksheetErrors.OosCaseNotFound(id));

        if (oosCase.Status != OosCaseStatus.Open)
            return Result.Failure<OosCaseDetailDto>(
                QcWorksheetErrors.StartInvestigationRequiresOpen(oosCase.Status));

        var subject = await LoadSubject(oosCase.WorksheetInstanceId);

        // The one deliberate write to live, shared, system-of-record state. A QC-only shadow
        // quarantine that Warehouse and Production could not see would be pharmaceutically
        // meaningless — see QcOosBatchDisposition for the full mapping and its caveats.
        if (subject?.MaterialBatchId is not null)
        {
            var batch = await context.MaterialBatches
                .SingleOrDefaultAsync(item => item.Id == subject.MaterialBatchId.Value);

            oosCase.QuarantinedMaterialBatchId = subject.MaterialBatchId;
            oosCase.QuarantinedFromBatchStatus = QcOosBatchDisposition.Quarantine(batch);
        }

        if (subject?.BatchManufacturingRecordId is not null)
        {
            var record = await context.BatchManufacturingRecords
                .SingleOrDefaultAsync(item => item.Id == subject.BatchManufacturingRecordId.Value);

            oosCase.QuarantinedBatchManufacturingRecordId = subject.BatchManufacturingRecordId;
            oosCase.QuarantinedFromBatchManufacturingStatus = QcOosBatchDisposition.Quarantine(record);
        }

        oosCase.Status = OosCaseStatus.InvestigationInProgress;
        oosCase.InvestigatedById = userId;
        oosCase.InvestigatedAt = DateTime.UtcNow;
        oosCase.UpdatedAt = DateTime.UtcNow;
        oosCase.LastUpdatedById = userId;

        await context.SaveChangesAsync();

        return await GetOosCase(id);
    }

    public async Task<Result<OosCaseDetailDto>> UpdateInvestigation(
        Guid id, UpdateOosInvestigationRequest request, Guid userId)
    {
        var oosCase = await context.QcOosCases.SingleOrDefaultAsync(item => item.Id == id);
        if (oosCase is null)
            return Result.Failure<OosCaseDetailDto>(QcWorksheetErrors.OosCaseNotFound(id));

        // Findings are editable only while the investigation is running. Once a retest is
        // authorized or QA has the case, the record they are acting on must stop moving.
        if (oosCase.Status != OosCaseStatus.InvestigationInProgress)
            return Result.Failure<OosCaseDetailDto>(
                QcWorksheetErrors.InvestigationNotEditable(oosCase.Status));

        oosCase.InvestigationDetails = request.InvestigationDetails?.Trim();
        oosCase.RootCauseAnalysis = request.RootCauseAnalysis?.Trim();
        oosCase.CorrectiveActions = request.CorrectiveActions?.Trim();
        oosCase.PreventiveActions = request.PreventiveActions?.Trim();
        oosCase.InvestigatedById = userId;
        oosCase.InvestigatedAt = DateTime.UtcNow;
        oosCase.UpdatedAt = DateTime.UtcNow;
        oosCase.LastUpdatedById = userId;

        await context.SaveChangesAsync();

        return await GetOosCase(id);
    }

    // -----------------------------------------------------------------------
    // Retest / escalation
    // -----------------------------------------------------------------------

    /// <summary>
    /// Authorizes a retest: lab error found, so the test runs again on a new, linked worksheet.
    /// <para>
    /// Which sample it runs against is the Specification's decision, not the caller's —
    /// <see cref="QcRetestPolicy.SameSample"/> reuses the original Subject,
    /// <see cref="QcRetestPolicy.FreshResample"/> creates a new one under the same round with
    /// its own collection time. Chemical assay retests are often same-sample and microbial
    /// often needs a fresh sample, which is exactly why this is configured per Specification
    /// rather than decided case by case.
    /// </para>
    /// </summary>
    public async Task<Result<OosCaseDetailDto>> AuthorizeRetest(
        Guid id, AuthorizeOosRetestRequest request, Guid userId)
    {
        var oosCase = await context.QcOosCases.SingleOrDefaultAsync(item => item.Id == id);
        if (oosCase is null)
            return Result.Failure<OosCaseDetailDto>(QcWorksheetErrors.OosCaseNotFound(id));

        if (oosCase.Status != OosCaseStatus.InvestigationInProgress)
            return Result.Failure<OosCaseDetailDto>(
                QcWorksheetErrors.RetestRequiresInvestigationInProgress(oosCase.Status));

        var original = await context.QcWorksheetInstances
            .Include(item => item.TestRequestSubject)
                .ThenInclude(subject => subject.TestRequest)
            .SingleOrDefaultAsync(item => item.Id == oosCase.WorksheetInstanceId);

        if (original?.TestRequestSubject?.TestRequest is null)
            return Result.Failure<OosCaseDetailDto>(
                QcWorksheetErrors.WorksheetInstanceNotFound(oosCase.WorksheetInstanceId));

        var round = original.TestRequestSubject.TestRequest;

        // The round's pinned Specification row, by id — not whichever version is Effective now.
        var specification = await context.QcSpecifications
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == round.SpecificationId);

        if (specification is null)
            return Result.Failure<OosCaseDetailDto>(
                QcWorksheetErrors.TestRequestSpecificationNotFound(round.SpecificationId));

        // Required with no default precisely so this branch never has to guess. The zero value
        // means "never set", which is a configuration gap, not a SameSample answer.
        if (!Enum.IsDefined(specification.RetestPolicy))
            return Result.Failure<OosCaseDetailDto>(
                QcWorksheetErrors.RetestPolicyNotSet(specification.Code));

        var subjectId = original.TestRequestSubjectId;

        if (specification.RetestPolicy == QcRetestPolicy.FreshResample)
        {
            var resample = BuildResampleSubject(original.TestRequestSubject, request, userId);
            context.QcTestRequestSubjects.Add(resample);
            subjectId = resample.Id;
        }

        var retest = new WorksheetInstance
        {
            Id = Guid.NewGuid(),
            TestRequestSubjectId = subjectId,
            WorksheetTemplateId = original.WorksheetTemplateId,

            // Hard version pinning: the retest runs the version the original ran, copied off the
            // original row. A template revised since the original was raised must not change
            // what the retest measures, or the two results would not be comparable.
            WorksheetTemplateVersion = original.WorksheetTemplateVersion,
            AnalysisType = original.AnalysisType,
            Status = WorksheetInstanceStatus.NotStarted,

            // The far side of the link. The original keeps its own values untouched.
            RetestOfInstanceId = original.Id,
            Approved = false,
            CreatedAt = DateTime.UtcNow,
            CreatedById = userId
        };

        context.QcWorksheetInstances.Add(retest);

        oosCase.RetestWorksheetInstanceId = retest.Id;
        oosCase.RetestAuthorizedById = userId;
        oosCase.RetestAuthorizedAt = DateTime.UtcNow;
        oosCase.Status = OosCaseStatus.RetestRequested;
        oosCase.UpdatedAt = DateTime.UtcNow;
        oosCase.LastUpdatedById = userId;

        if (!string.IsNullOrWhiteSpace(request?.Reason))
            oosCase.InvestigationDetails = Append(oosCase.InvestigationDetails, request.Reason.Trim());

        await context.SaveChangesAsync();

        // The round reflects that there is unfinished work again.
        await QcTestRequestStatusCalculator.RecalculateAsync(context, round.Id);

        return await GetOosCase(id);
    }

    /// <summary>
    /// Escalates straight to QA: no lab error was found, so there is nothing a retest would
    /// settle. The QA approval round opens immediately, since unlike the retest path there is
    /// no completion to wait for.
    /// </summary>
    public async Task<Result<OosCaseDetailDto>> Escalate(
        Guid id, EscalateOosCaseRequest request, Guid userId)
    {
        var oosCase = await context.QcOosCases.SingleOrDefaultAsync(item => item.Id == id);
        if (oosCase is null)
            return Result.Failure<OosCaseDetailDto>(QcWorksheetErrors.OosCaseNotFound(id));

        if (oosCase.Status != OosCaseStatus.InvestigationInProgress)
            return Result.Failure<OosCaseDetailDto>(
                QcWorksheetErrors.EscalateRequiresInvestigationInProgress(oosCase.Status));

        // Same opt-out of the approval engine's silent auto-approval fallback that Milestones
        // 1-3 take: a batch must never be rejected or released with nobody having signed for it.
        if (!await HasConfiguredApprovalChain())
            return Result.Failure<OosCaseDetailDto>(
                QcWorksheetErrors.NoApprovalWorkflowConfigured(ModelType));

        oosCase.Status = OosCaseStatus.PendingQaDisposition;
        oosCase.UpdatedAt = DateTime.UtcNow;
        oosCase.LastUpdatedById = userId;

        if (!string.IsNullOrWhiteSpace(request?.Reason))
            oosCase.InvestigationDetails = Append(oosCase.InvestigationDetails, request.Reason.Trim());

        await context.SaveChangesAsync();

        await approvalRepository.CreateInitialApprovalsAsync(ModelType, id);

        return await GetOosCase(id);
    }

    // -----------------------------------------------------------------------
    // QA disposition
    // -----------------------------------------------------------------------

    /// <summary>
    /// The QA disposition, and the only action in this module that rejects or releases a real
    /// batch.
    /// <para>
    /// The sequence is: readiness check, then the Milestone 1 re-authentication wrapper calling
    /// the same <c>ApproveItem</c> every other QC approval uses, then — only once the approval
    /// engine reports every required stage signed — the batch status write and the close. That
    /// ordering is what stops the first signature of a multi-stage QA chain from releasing a
    /// batch on its own.
    /// </para>
    /// </summary>
    public async Task<Result<OosCaseDetailDto>> RecordDisposition(
        Guid id, OosDispositionRequest request, Guid userId, List<Guid> roleIds)
    {
        var oosCase = await context.QcOosCases.SingleOrDefaultAsync(item => item.Id == id);
        if (oosCase is null)
            return Result.Failure<OosCaseDetailDto>(QcWorksheetErrors.OosCaseNotFound(id));

        if (oosCase.Status != OosCaseStatus.PendingQaDisposition)
            return Result.Failure<OosCaseDetailDto>(
                QcWorksheetErrors.DispositionRequiresPendingQa(oosCase.Status));

        if (!request.Outcome.HasValue || !Enum.IsDefined(request.Outcome.Value))
            return Result.Failure<OosCaseDetailDto>(QcWorksheetErrors.DispositionOutcomeRequired);

        var outcome = request.Outcome.Value;

        // Accepting a retest result requires there to be one.
        if (outcome == OosDispositionOutcome.RetestAccepted && !oosCase.RetestWorksheetInstanceId.HasValue)
            return Result.Failure<OosCaseDetailDto>(QcWorksheetErrors.RetestAcceptedRequiresRetest);

        // The readiness check carried forward from the live OosInvestigation: every worksheet
        // for this sample must be finished before QA decides the batch's fate.
        var readiness = await CheckReadiness(oosCase);
        if (!readiness.IsSuccess)
            return Result.Failure<OosCaseDetailDto>(readiness.Error);

        // Recorded before signing, so the signature is taken against a stated decision rather
        // than the decision being backfilled onto an anonymous approval.
        oosCase.DispositionOutcome = outcome;
        oosCase.DispositionById = userId;
        oosCase.DispositionAt = DateTime.UtcNow;
        oosCase.DispositionComments = request.DispositionComments?.Trim();
        oosCase.UpdatedAt = DateTime.UtcNow;
        oosCase.LastUpdatedById = userId;
        await context.SaveChangesAsync();

        // The QC re-authentication wrapper: verify the caller's own password, then the same
        // ApproveItem the generic approval endpoint calls, which writes the QcApproval row
        // (EntityType = "OosCase") with ReauthConfirmedAt set.
        var signed = await signatureService.SignAndApproveAsync(
            ModelType, id, userId, roleIds, request.Password, request.DispositionComments);

        if (!signed.IsSuccess)
        {
            // The decision never happened. Roll the recorded intent back so a failed or refused
            // signature does not leave the case looking disposed.
            oosCase.DispositionOutcome = null;
            oosCase.DispositionById = null;
            oosCase.DispositionAt = null;
            oosCase.DispositionComments = null;
            await context.SaveChangesAsync();

            return Result.Failure<OosCaseDetailDto>(signed.Error);
        }

        // Approved becomes true only when every required stage has signed. Until then the case
        // stays PendingQaDisposition and no batch moves.
        //
        // Read straight off the tracked entity: the approval handler ran inside this same
        // DbContext and, by EF Core identity resolution, updated this very instance. Re-reading
        // it no-tracking would risk writing a stale Approved back on the next SaveChanges.
        if (oosCase.Approved)
            await CloseAsync(oosCase, outcome, userId);

        return await GetOosCase(id);
    }

    /// <summary>
    /// Applies the outcome to the real batch and closes the case.
    /// <para>
    /// The quarantine links are cleared on close: the case is no longer holding anything, and
    /// leaving them set would misreport a closed case as still quarantining a batch. What the
    /// case did is preserved in <c>DispositionOutcome</c> and in the QcApproval signature trail.
    /// </para>
    /// </summary>
    private async Task CloseAsync(OosCase oosCase, OosDispositionOutcome outcome, Guid userId)
    {
        if (oosCase.QuarantinedMaterialBatchId.HasValue)
        {
            var batch = await context.MaterialBatches
                .SingleOrDefaultAsync(item => item.Id == oosCase.QuarantinedMaterialBatchId.Value);

            QcOosBatchDisposition.Dispose(batch, outcome);
        }

        if (oosCase.QuarantinedBatchManufacturingRecordId.HasValue)
        {
            var record = await context.BatchManufacturingRecords
                .SingleOrDefaultAsync(
                    item => item.Id == oosCase.QuarantinedBatchManufacturingRecordId.Value);

            QcOosBatchDisposition.Dispose(record, outcome);
        }

        oosCase.Status = OosCaseStatus.Closed;
        oosCase.QuarantinedMaterialBatchId = null;
        oosCase.QuarantinedBatchManufacturingRecordId = null;
        oosCase.UpdatedAt = DateTime.UtcNow;
        oosCase.LastUpdatedById = userId;

        await context.SaveChangesAsync();

        var testRequestId = await TestRequestIdFor(oosCase.WorksheetInstanceId);
        if (testRequestId != Guid.Empty)
            await QcTestRequestStatusCalculator.RecalculateAsync(context, testRequestId);
    }

    /// <summary>
    /// Every worksheet under the case's own Subject must be Reviewed or Locked, and so must the
    /// retest if one was authorized.
    /// <para>
    /// The Subject check is the rule the live <c>OosInvestigation.ReviewByQa</c> already
    /// enforces. The retest check extends it by one case that the Subject check alone would
    /// miss: under <see cref="QcRetestPolicy.FreshResample"/> the retest lives under a
    /// <i>different</i> Subject, so without this a case could be disposed while the very retest
    /// it is waiting on was still in progress.
    /// </para>
    /// </summary>
    private async Task<Result> CheckReadiness(OosCase oosCase)
    {
        var subjectId = await context.QcWorksheetInstances
            .AsNoTracking()
            .Where(item => item.Id == oosCase.WorksheetInstanceId)
            .Select(item => item.TestRequestSubjectId)
            .SingleOrDefaultAsync();

        var pending = await context.QcWorksheetInstances
            .AsNoTracking()
            .Where(item => (item.TestRequestSubjectId == subjectId
                    || (oosCase.RetestWorksheetInstanceId.HasValue
                        && item.Id == oosCase.RetestWorksheetInstanceId.Value))
                && item.Status != WorksheetInstanceStatus.Reviewed
                && item.Status != WorksheetInstanceStatus.Locked)
            .Select(item => new { item.Status, Code = item.WorksheetTemplate.Code })
            .ToListAsync();

        if (pending.Count == 0)
            return Result.Success();

        var first = pending[0];
        return QcWorksheetErrors.DispositionBlockedByIncompleteWork(first.Code, first.Status);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// A fresh sample of the same thing: the batch links, sampling point and labels carry over
    /// because it is the same material from the same place, but the collection time is new —
    /// that is what makes it a fresh sample rather than a copy of the old one.
    /// <para>
    /// Deliberately created <b>without</b> the full set of WorksheetInstances a Subject
    /// normally materializes. A retest re-runs the one worksheet that failed, not the entire
    /// analysis; raising the untouched tracks again would invent work nobody asked for and
    /// would make the round look incomplete for as long as it sat unassigned.
    /// </para>
    /// </summary>
    private static TestRequestSubject BuildResampleSubject(
        TestRequestSubject original, AuthorizeOosRetestRequest request, Guid userId) => new()
    {
        Id = Guid.NewGuid(),
        TestRequestId = original.TestRequestId,
        SubjectRef = original.SubjectRef,
        SubjectLabel = original.SubjectLabel,
        ArNumber = string.IsNullOrWhiteSpace(request?.ArNumber)
            ? original.ArNumber
            : request.ArNumber.Trim(),
        SamplingPointGroupId = original.SamplingPointGroupId,
        SamplingPointId = original.SamplingPointId,
        MaterialBatchId = original.MaterialBatchId,
        BatchManufacturingRecordId = original.BatchManufacturingRecordId,
        CollectedAt = request?.CollectedAt ?? DateTime.UtcNow,
        CreatedAt = DateTime.UtcNow,
        CreatedById = userId
    };

    /// <summary>
    /// Delegates to <see cref="QcReleaseHold"/> rather than re-deriving the rule: the TestRequest
    /// detail reports the same hold as <c>BlocksRelease</c>, and two implementations of "is this
    /// round held" would be two chances for the screen and the gate to disagree about whether a
    /// round is still under investigation.
    /// </summary>
    private Task<int> CountOpenCases(Guid testRequestId) =>
        QcReleaseHold.CountOpenCasesAsync(context, testRequestId);

    private async Task<TestRequestSubject> LoadSubject(Guid worksheetInstanceId) =>
        await context.QcWorksheetInstances
            .AsNoTracking()
            .Where(item => item.Id == worksheetInstanceId)
            .Select(item => item.TestRequestSubject)
            .SingleOrDefaultAsync();

    private async Task<Guid> TestRequestIdFor(Guid worksheetInstanceId) =>
        await context.QcWorksheetInstances
            .AsNoTracking()
            .Where(item => item.Id == worksheetInstanceId)
            .Select(item => item.TestRequestSubject.TestRequestId)
            .SingleOrDefaultAsync();

    private async Task<bool> HasConfiguredApprovalChain()
    {
        var approval = await context.Approvals.FirstOrDefaultAsync(item => item.ItemType == ModelType);
        if (approval is null)
            return false;

        return await context.ApprovalStages.AnyAsync(stage => stage.ApprovalId == approval.Id);
    }

    private static string Append(string existing, string addition) =>
        string.IsNullOrWhiteSpace(existing) ? addition : $"{existing}{Environment.NewLine}{addition}";

    private async Task<OosCase> LoadDetail(Guid id) =>
        await context.QcOosCases
            .Include(item => item.SpecificationCharacteristic)
            .Include(item => item.InvestigatedBy)
            .Include(item => item.RetestAuthorizedBy)
            .Include(item => item.DispositionBy)
            .Include(item => item.WorksheetInstance)
                .ThenInclude(instance => instance.WorksheetTemplate)
            .Include(item => item.WorksheetInstance)
                .ThenInclude(instance => instance.AssignedTo)
            .Include(item => item.WorksheetInstance)
                .ThenInclude(instance => instance.TestRequestSubject)
                    .ThenInclude(subject => subject.TestRequest)
                        .ThenInclude(request => request.Specification)
            .Include(item => item.RetestWorksheetInstance)
                .ThenInclude(instance => instance.WorksheetTemplate)
            .Include(item => item.RetestWorksheetInstance)
                .ThenInclude(instance => instance.AssignedTo)
            .AsSplitQuery()
            .SingleOrDefaultAsync(item => item.Id == id);

    // -----------------------------------------------------------------------
    // Projection
    // -----------------------------------------------------------------------

    private OosCaseSummaryDto ToSummaryDto(OosCase oosCase) =>
        Fill(new OosCaseSummaryDto(), oosCase);

    private TDto Fill<TDto>(TDto dto, OosCase oosCase) where TDto : OosCaseSummaryDto
    {
        var instance = oosCase.WorksheetInstance;
        var subject = instance?.TestRequestSubject;
        var round = subject?.TestRequest;

        dto.Id = oosCase.Id;
        dto.WorksheetInstanceId = oosCase.WorksheetInstanceId;
        dto.FieldKey = oosCase.FieldKey;
        dto.Status = oosCase.Status;
        dto.OpenedAt = oosCase.OpenedAt;
        dto.ObservedValue = oosCase.ObservedValue;
        dto.BreachedLimit = oosCase.BreachedLimit;
        dto.TestName = oosCase.SpecificationCharacteristic?.TestName;
        dto.TestRequestSubjectId = instance?.TestRequestSubjectId ?? Guid.Empty;
        dto.SubjectRef = subject?.SubjectRef;
        dto.SubjectLabel = subject?.SubjectLabel;
        dto.TestRequestId = subject?.TestRequestId ?? Guid.Empty;
        dto.ArNumber = round?.ArNumber;
        dto.WorksheetTemplateCode = instance?.WorksheetTemplate?.Code;
        dto.Approved = oosCase.Approved;
        dto.DispositionOutcome = oosCase.DispositionOutcome;
        dto.RetestWorksheetInstanceId = oosCase.RetestWorksheetInstanceId;
        dto.CreatedAt = oosCase.CreatedAt;
        return dto;
    }

    private async Task<OosCaseDetailDto> ToDetailDto(OosCase oosCase)
    {
        var dto = Fill(new OosCaseDetailDto(), oosCase);

        var instance = oosCase.WorksheetInstance;
        var round = instance?.TestRequestSubject?.TestRequest;
        var characteristic = oosCase.SpecificationCharacteristic;

        if (round is not null)
        {
            dto.SpecificationId = round.SpecificationId;

            // The pin as stored, not re-read from the Specification row: a drifted pin should be
            // visible rather than quietly corrected on the way out.
            dto.SpecificationVersion = round.SpecificationVersion;
            dto.SpecificationCode = round.Specification?.Code;
            dto.RetestPolicy = round.Specification?.RetestPolicy ?? default;
            dto.BlocksRelease = oosCase.Status != OosCaseStatus.Closed;
        }

        dto.SpecificationCharacteristicId = oosCase.SpecificationCharacteristicId;
        dto.AcceptanceCriteria = characteristic?.AcceptanceCriteria;
        dto.AlertLimit = characteristic?.AlertLimit;
        dto.ActionLimit = characteristic?.ActionLimit;

        dto.InvestigationDetails = oosCase.InvestigationDetails;
        dto.RootCauseAnalysis = oosCase.RootCauseAnalysis;
        dto.CorrectiveActions = oosCase.CorrectiveActions;
        dto.PreventiveActions = oosCase.PreventiveActions;
        dto.InvestigatedBy = Map(oosCase.InvestigatedBy);
        dto.InvestigatedAt = oosCase.InvestigatedAt;

        dto.RetestAuthorizedBy = Map(oosCase.RetestAuthorizedBy);
        dto.RetestAuthorizedAt = oosCase.RetestAuthorizedAt;

        // Both instances, side by side. The original is never replaced by its retest in this
        // view — that is the whole point of a linked retest rather than a reopened record.
        dto.WorksheetInstance = instance is null
            ? null
            : QcWorksheetInstanceMapper.ToSummaryDto(instance, mapper);

        dto.RetestWorksheetInstance = oosCase.RetestWorksheetInstance is null
            ? null
            : QcWorksheetInstanceMapper.ToSummaryDto(oosCase.RetestWorksheetInstance, mapper);

        dto.DispositionBy = Map(oosCase.DispositionBy);
        dto.DispositionAt = oosCase.DispositionAt;
        dto.DispositionComments = oosCase.DispositionComments;
        dto.QuarantinedMaterialBatchId = oosCase.QuarantinedMaterialBatchId;
        dto.QuarantinedBatchManufacturingRecordId = oosCase.QuarantinedBatchManufacturingRecordId;

        return await Task.FromResult(dto);
    }

    private UserDto Map(User user) => user is null ? null : mapper.Map<UserDto>(user);
}
