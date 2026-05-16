using APP.Utils;
using DOMAIN.Entities.PayrollValidationIssues;
using SHARED;

namespace APP.IRepository;

public interface IPayrollValidationIssueRepository
{
    Task<Result<Paginateable<IEnumerable<PayrollValidationIssueDto>>>> GetValidationIssues(Guid payrollRunId, int page, int pageSize,
        string searchQuery, PayrollValidationStage? stage = null, PayrollValidationSeverity? severity = null,
        PayrollValidationIssueStatus? status = null);
    Task<Result<PayrollValidationIssueDto>> GetValidationIssue(Guid id);
    Task<Result> ResolveValidationIssue(Guid id, ResolveValidationIssueRequest request, Guid userId);
}
