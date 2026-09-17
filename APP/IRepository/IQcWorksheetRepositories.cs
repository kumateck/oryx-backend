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
    /// Every field on the current Effective version of each linked worksheet template —
    /// what the SourceFieldKey dropdown is populated from, and the same resolution the
    /// SourceFieldKey validation uses.
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
