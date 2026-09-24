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
