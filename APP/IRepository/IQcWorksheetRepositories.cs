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
