using APP.Utils;
using DOMAIN.Entities.PayrollRunEmployees;
using SHARED;

namespace APP.IRepository;

public interface IPayrollRunEmployeeRepository
{
    Task<Result<Paginateable<IEnumerable<PayrollRunEmployeeDto>>>> GetRunEmployees(Guid payrollRunId, int page, int pageSize,
        string searchQuery,
        bool? isExcluded = null);
    Task<Result<PayrollRunEmployeeDto>> GetRunEmployee(Guid id);
}