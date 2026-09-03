using APP.Utils;
using DOMAIN.Entities.QualityAudits;
using SHARED;

namespace APP.IRepository;

public interface IQualityAuditRepository
{
    Task<Result<Guid>> CreateAudit(CreateQualityAuditRequest request, Guid userId);
    Task<Result> UpdateAudit(Guid auditId, UpdateQualityAuditRequest request, Guid userId);
    Task<Result> StartAudit(Guid auditId, Guid userId);

    Task<Result<Guid>> RecordChecklistResponse(Guid auditId, RecordChecklistResponseRequest request, Guid userId);
    Task<Result<Guid>> RaiseFinding(Guid auditId, RaiseFindingRequest request, Guid userId);
    Task<Result<Guid>> RaiseCorrectiveAction(Guid findingId, RaiseCorrectiveActionRequest request, Guid userId);
    Task<Result> UpdateCorrectiveActionStatus(Guid capaId, UpdateCorrectiveActionStatusRequest request, Guid userId);
    Task<Result> VerifyCorrectiveActionEffectiveness(Guid capaId, VerifyCorrectiveActionEffectivenessRequest request, Guid userId);

    Task<Result> SubmitForClosure(Guid auditId, SubmitAuditForClosureRequest request, Guid userId);
    Task<Result> CloseAudit(Guid auditId, CloseQualityAuditRequest request, Guid userId);

    Task<Result<QualityAuditDto>> GetAudit(Guid id);
    Task<Result<Paginateable<IEnumerable<QualityAuditDto>>>> GetAudits(
        int page,
        int pageSize,
        string searchQuery,
        AuditStatus? status,
        AuditType? type
    );

    Task<Result<List<AuditChecklistTemplateDto>>> GetChecklistTemplates();
    Task<Result<byte[]>> GenerateAuditReportPdf(Guid auditId);
}
