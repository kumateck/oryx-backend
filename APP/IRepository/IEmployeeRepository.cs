using APP.Utils;
using DOMAIN.Entities.Employees;
using SHARED;

namespace APP.IRepository;

public interface IEmployeeRepository
{
   Task<Result> OnboardEmployees(OnboardEmployeeDto employeeDto);
   Task<Result<Guid?>> CreateEmployee(CreateEmployeeRequest request, Guid userId);
   Task<Result<Paginateable<IEnumerable<EmployeeDto>>>> GetEmployees(int page, int pageSize,
      string searchQuery = null, string designation = null, string department = null);
   Task<Result<EmployeeDto>> GetEmployee(Guid id);
   Task<Result> UpdateEmployee(Guid id, CreateEmployeeRequest request, Guid userId);
   
   Task<Result> AssignEmployee(Guid id, AssignEmployeeDto employeeDto, Guid userId);
   
   Task<Result> DeleteEmployee(Guid id, Guid userId);
}