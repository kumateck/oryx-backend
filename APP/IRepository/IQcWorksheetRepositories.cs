using APP.Utils;
using DOMAIN.Entities.QcWorksheets;
using Microsoft.AspNetCore.Http;
using SHARED;

namespace APP.IRepository;

public interface IStandardTestProcedureRepository
{
    Task<Result<Paginateable<IEnumerable<StpSummaryDto>>>> GetStps(
        int page, int pageSize, string searchQuery, QcDocumentStatus? status);

    Task<Result<StpDetailDto>> GetStp(Guid id);

    Task<Result<StpDetailDto>> CreateStp(CreateStpRequest request, Guid userId);

    Task<Result<StpDetailDto>> UpdateStp(Guid id, UpdateStpRequest request, Guid userId);

    Task<Result<StpDetailDto>> CreateNewVersion(Guid id, Guid userId);

    Task<Result<StpDetailDto>> SubmitForReview(Guid id, Guid userId);

    Task<Result<StpDetailDto>> Approve(
        Guid id, QcApprovalRequest request, Guid userId, List<Guid> roleIds);

    Task<Result<StpDetailDto>> Reject(
        Guid id, QcApprovalRequest request, Guid userId, List<Guid> roleIds);

    Task<Result<StpDetailDto>> MakeEffective(Guid id, Guid userId);

    Task<Result<StpDetailDto>> Supersede(Guid id, QcSupersedeRequest request, Guid userId);

    Task<Result<List<StpImportResultDto>>> Import(IFormFileCollection files, Guid userId);
}

public interface IWorksheetTemplateRepository
{
    Task<Result<Paginateable<IEnumerable<WorksheetTemplateSummaryDto>>>> GetTemplates(
        int page, int pageSize, string searchQuery, QcDocumentStatus? status);

    Task<Result<WorksheetTemplateDetailDto>> GetTemplate(Guid id);

    Task<Result<WorksheetTemplateDetailDto>> CreateTemplate(
        CreateWorksheetTemplateRequest request, Guid userId);

    Task<Result<WorksheetTemplateDetailDto>> UpdateTemplate(
        Guid id, UpdateWorksheetTemplateRequest request, Guid userId);

    Task<Result<WorksheetTemplateDetailDto>> CreateNewVersion(Guid id, Guid userId);

    Task<Result<WorksheetTemplateDetailDto>> SubmitForReview(Guid id, Guid userId);

    Task<Result<WorksheetTemplateDetailDto>> Approve(
        Guid id, QcApprovalRequest request, Guid userId, List<Guid> roleIds);

    Task<Result<WorksheetTemplateDetailDto>> Reject(
        Guid id, QcApprovalRequest request, Guid userId, List<Guid> roleIds);

    Task<Result<WorksheetTemplateDetailDto>> MakeEffective(Guid id, Guid userId);

    Task<Result<WorksheetTemplateDetailDto>> Supersede(
        Guid id, QcSupersedeRequest request, Guid userId);
}

public interface ISpecificationRepository
{
    Task<Result<Paginateable<IEnumerable<SpecificationSummaryDto>>>> GetSpecifications(
        int page, int pageSize, string searchQuery, QcDocumentStatus? status,
        SpecificationAppliesTo? appliesTo);

    Task<Result<SpecificationDetailDto>> GetSpecification(Guid id);

    Task<Result<SpecificationDetailDto>> CreateSpecification(
        CreateSpecificationRequest request, Guid userId);

    Task<Result<SpecificationDetailDto>> UpdateSpecification(
        Guid id, UpdateSpecificationRequest request, Guid userId);

    Task<Result<SpecificationDetailDto>> CreateNewVersion(Guid id, Guid userId);

    Task<Result<SpecificationDetailDto>> SubmitForReview(Guid id, Guid userId);

    Task<Result<SpecificationDetailDto>> Approve(
        Guid id, QcApprovalRequest request, Guid userId, List<Guid> roleIds);

    Task<Result<SpecificationDetailDto>> Reject(
        Guid id, QcApprovalRequest request, Guid userId, List<Guid> roleIds);

    Task<Result<SpecificationDetailDto>> MakeEffective(Guid id, Guid userId);

    Task<Result<SpecificationDetailDto>> Supersede(
        Guid id, QcSupersedeRequest request, Guid userId);

    /// <summary>
    /// Every field on the <b>pinned</b> version of each linked worksheet template — what the
    /// SourceFieldKey dropdown is populated from, and the same resolution the SourceFieldKey
    /// validation uses, so the dropdown can only ever offer a field that will validate.
    /// </summary>
    Task<Result<List<SpecificationAvailableFieldDto>>> GetAvailableFields(Guid id);
}

public interface ISamplingPointGroupRepository
{
    Task<Result<List<SamplingPointGroupDto>>> GetSamplingPointGroups(string searchQuery);

    Task<Result<SamplingPointGroupDto>> GetSamplingPointGroup(Guid id);

    Task<Result<SamplingPointGroupDto>> CreateSamplingPointGroup(
        CreateSamplingPointGroupRequest request, Guid userId);

    Task<Result<SamplingPointGroupDto>> UpdateSamplingPointGroup(
        Guid id, UpdateSamplingPointGroupRequest request, Guid userId);

    Task<Result> DeleteSamplingPointGroup(Guid id, Guid userId);
}

public interface ITestRequestRepository
{
    Task<Result<Paginateable<IEnumerable<TestRequestSummaryDto>>>> GetTestRequests(
        int page, int pageSize, string searchQuery, TestRequestStatus? status, TestRequestType? type,
        DateTime? from, DateTime? to);

    Task<Result<TestRequestDetailDto>> GetTestRequest(Guid id);

    /// <summary>
    /// Creates the round and, in the same transaction, every WorksheetInstance it implies —
    /// one per (Subject × the pinned Specification's WorksheetLink).
    /// </summary>
    Task<Result<TestRequestDetailDto>> CreateTestRequest(CreateTestRequestRequest request, Guid userId);

    /// <summary>
    /// Adds Subjects to a round that has not yet started testing, each getting its own
    /// WorksheetInstances off the round's already-pinned Specification version.
    /// </summary>
    Task<Result<TestRequestDetailDto>> AddSubjects(
        Guid id, AddTestRequestSubjectsRequest request, Guid userId);

    Task<Result<TestRequestDetailDto>> RecordSample(
        Guid id, RecordTestRequestSampleRequest request, Guid userId);
}

public interface IWorksheetInstanceRepository
{
    /// <summary>
    /// Full detail: the computed header block, the pinned template version's sections and
    /// fields merged with recorded values, and runtime-resolved ReferencedResult fields.
    /// </summary>
    Task<Result<WorksheetInstanceDetailDto>> GetWorksheetInstance(Guid id);

    /// <summary>
    /// Which analysis track a worksheet belongs to. The controller needs this before it can
    /// decide which of the paired Chemical/Microbial permission keys the action requires —
    /// the keys are split per track precisely so a microbiologist is never granted chemistry
    /// actions, and that choice cannot be made from the route alone.
    /// </summary>
    Task<Result<SpecificationAnalysisType>> GetAnalysisType(Guid id);

    /// <summary>The Test Room's "My Work" — only the instances assigned to this user.</summary>
    Task<Result<WorksheetQueueDto>> GetMyWork(Guid userId, SpecificationAnalysisType? analysisType);

    /// <summary>"Awaiting My Review" — everything submitted on the given track.</summary>
    Task<Result<WorksheetQueueDto>> GetReviewQueue(SpecificationAnalysisType? analysisType);

    Task<Result<WorksheetInstanceDetailDto>> Assign(
        Guid id, AssignWorksheetInstanceRequest request, Guid userId);

    /// <summary>
    /// Moves the work to someone else and writes a plain audit row. Never rewrites
    /// <c>EnteredBy</c> on values already entered, never resets Status, and never creates a
    /// QcApproval row — reassignment is administrative, not a signature.
    /// </summary>
    Task<Result<WorksheetInstanceDetailDto>> Reassign(
        Guid id, ReassignWorksheetInstanceRequest request, Guid userId);

    Task<Result<WorksheetInstanceDetailDto>> Start(Guid id, Guid userId);

    Task<Result<WorksheetInstanceDetailDto>> SaveValues(
        Guid id, SaveWorksheetValuesRequest request, Guid userId);

    Task<Result<WorksheetInstanceDetailDto>> Submit(Guid id, Guid userId);

    Task<Result<WorksheetInstanceDetailDto>> Review(
        Guid id, ReviewWorksheetInstanceRequest request, Guid userId, List<Guid> roleIds);

    Task<Result<WorksheetInstanceDetailDto>> ReturnForCorrection(
        Guid id, ReturnWorksheetForCorrectionRequest request, Guid userId);
}

/// <summary>
/// The formal OOS/OOT workflow (Milestone 4): Phase 1 investigation, retest or escalation, and
/// the QA disposition that rejects or releases the real batch.
/// </summary>
public interface IOosCaseRepository
{
    Task<Result<Paginateable<IEnumerable<OosCaseSummaryDto>>>> GetOosCases(
        int page, int pageSize, string searchQuery, OosCaseStatus? status);

    /// <summary>
    /// Full detail: the breach in context against the round's <b>pinned</b> Specification
    /// version, the investigation, and both the original and retest worksheets side by side.
    /// </summary>
    Task<Result<OosCaseDetailDto>> GetOosCase(Guid id);

    /// <summary>
    /// Whether an unclosed OOS case is holding this round back from Released. The single source
    /// of truth for the release block — Milestone 5's certificate flow asks this rather than
    /// re-deriving the rule.
    /// </summary>
    Task<Result<bool>> IsReleaseBlocked(Guid testRequestId);

    /// <summary>
    /// Opens Phase 1 and quarantines the linked batch. Quarantine happens <b>here</b>, not at
    /// auto-creation: locking a batch out of use across the whole ERP deserves a human
    /// confirmation first.
    /// </summary>
    Task<Result<OosCaseDetailDto>> StartInvestigation(Guid id, Guid userId);

    Task<Result<OosCaseDetailDto>> UpdateInvestigation(
        Guid id, UpdateOosInvestigationRequest request, Guid userId);

    /// <summary>
    /// Creates the retest as a <b>new</b> WorksheetInstance linked by <c>RetestOfInstanceId</c>,
    /// pinned to the original's own template version. The original's values are never touched.
    /// Which sample it runs against follows the Specification's <see cref="QcRetestPolicy"/>.
    /// </summary>
    Task<Result<OosCaseDetailDto>> AuthorizeRetest(
        Guid id, AuthorizeOosRetestRequest request, Guid userId);

    Task<Result<OosCaseDetailDto>> Escalate(Guid id, EscalateOosCaseRequest request, Guid userId);

    /// <summary>
    /// The QA disposition, signed through the shared QcApproval table and the re-authentication
    /// wrapper. Writes the real batch status only once every required stage has signed.
    /// </summary>
    Task<Result<OosCaseDetailDto>> RecordDisposition(
        Guid id, OosDispositionRequest request, Guid userId, List<Guid> roleIds);
}

public interface IQcApprovalRepository
{
    /// <summary>
    /// The centralized QC approvals queue: every pending QcApproval across every EntityType,
    /// which is the point of keeping them in one table.
    /// </summary>
    Task<Result<List<QcPendingApprovalDto>>> GetPendingApprovals(Guid userId, List<Guid> roleIds);

    /// <summary>The full signature trail for one QC document.</summary>
    Task<Result<List<QcApprovalDto>>> GetApprovalsForEntity(string entityType, Guid entityId);
}

/// <summary>
/// Certificates (Milestone 5): the viewer, issuance and revision.
/// <para>
/// There is deliberately no create and no update method. A <c>Coa</c> is produced by
/// <c>IQcCoaGenerationService</c> when the round it certifies passes the strict-hold gate, and
/// the only two things a user does to one afterwards are issue it and revise it.
/// </para>
/// </summary>
public interface ICoaRepository
{
    Task<Result<Paginateable<IEnumerable<CoaSummaryDto>>>> GetCoas(
        int page,
        int pageSize,
        string searchQuery,
        CoaStatus? status,
        CoaCertificateShape? shape,
        DateTime? from,
        DateTime? to);

    /// <summary>
    /// The rendered certificate: the shape's own header block and the snapshotted rows, grouped by
    /// Subject and GroupName. Nothing here is re-derived from the Specification or the
    /// WorksheetInstances — every value was captured at generation time.
    /// </summary>
    Task<Result<CoaDetailDto>> GetCoa(Guid id);

    /// <summary>
    /// Draft to Issued. Locks the worksheets the certificate draws on and releases the round.
    /// Carries no re-authentication: the reviews that gated generation were each signed already.
    /// </summary>
    Task<Result<CoaDetailDto>> Issue(Guid id, Guid userId);

    /// <summary>
    /// Creates a replacement Draft with rows recomputed from current data. The original is
    /// untouched and only becomes Superseded once the replacement is itself issued.
    /// </summary>
    Task<Result<CoaDetailDto>> Revise(Guid id, ReviseCoaRequest request, Guid userId);
}
