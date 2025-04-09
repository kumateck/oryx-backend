using APP.Utils;
using DOMAIN.Entities.Employees;
using SHARED;

namespace APP.IRepository;

public interface IEmployeeRepository
{
   Task<Result> OnboardEmployee(OnboardEmployeeDto employeeDto);
   Task<Result<Guid?>> CreateEmployee(CreateEmployeeRequest request, Guid userId);
   Task<Result<Paginateable<IEnumerable<EmployeeDto>>>> GetEmployees(int page, int pageSize,
      string searchQuery);
   Task<Result<EmployeeDto>> GetEmployee(Guid id);
   Task<Result> UpdateEmployee(Guid id, CreateEmployeeRequest request, Guid userId);
}