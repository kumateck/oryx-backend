using APP.Utils;
using DOMAIN.Entities.PayrollRuns;
using SHARED;

namespace APP.IRepository;

public interface IPayrollRunRepository
{
    Task<Result<Guid>> CreatePayrollRun(CreatePayrollRunRequest request);
    Task<Result<Paginateable<IEnumerable<PayrollRunSummaryDto>>>> GetPayrollRuns(int page, int pageSize, string searchQuery,
        Guid? payrollCompanyId = null, Guid? payGroupId = null, PayrollRunStatus? status = null);
    Task<Result<PayrollRunDto>> GetPayrollRun(Guid id);
    Task<Result> TransitionPayrollRun(Guid id, PayrollRunStatus targetStatus, 
        PayrollRunTransitionRequest request, Guid userId);
    Task<Result> CancelPayrollRun(Guid id, CancelPayrollRunRequest request, Guid userId);
    Task<Result> DeletePayrollRun(Guid id, Guid userId);
}