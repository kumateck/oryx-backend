using APP.Utils;
using DOMAIN.Entities.PayRollPeriods;
using SHARED;

namespace APP.IRepository;

public interface IPayrollPeriodRepository
{
    Task<Result<Guid>> CreatePayrollPeriod(CreatePayrollPeriodRequest request);
    Task<Result<Paginateable<IEnumerable<PayrollPeriodDto>>>> GetPayrollPeriods(int page, int pageSize,
        string searchQuery, Guid? payrollCompanyId = null, PayrollPeriodStatus? status = null);
    Task<Result<PayrollPeriodDto>> GetPayrollPeriod(Guid id);
    Task<Result> UpdatePayrollPeriod(Guid id, CreatePayrollPeriodRequest request);
    Task<Result> ClosePayrollPeriod(ClosePayrollPeriodRequest request, Guid userId);
    Task<Result> ReopenPayrollPeriod(ReopenPayrollPeriodRequest request, Guid userId);
    Task<Result> DeletePayrollPeriod(Guid id, Guid userId);
}