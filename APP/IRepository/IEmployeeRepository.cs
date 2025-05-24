using APP.Utils;
using DOMAIN.Entities.Employees;
using DOMAIN.Entities.Users;
using Microsoft.AspNetCore.Http;
using SHARED;

namespace APP.IRepository;

public interface IEmployeeRepository
{
   Task<Result> OnboardEmployees(OnboardEmployeeDto employeeDto);
   
   Task<Result<Guid>> CreateEmployee(CreateEmployeeRequest request);
   
   Task<Result> CreateEmployeeUser(EmployeeUserDto employeeUserDto);
   Task<Result<Paginateable<IEnumerable<EmployeeDto>>>> GetEmployees(int page, int pageSize,
      string searchQuery = null, string designation = null, string department = null);
   
   Task<Result<IEnumerable<EmployeeDto>>> GetEmployeesByDepartment(Guid departmentId);
   Task<Result<EmployeeDto>> GetEmployee(Guid id);
   Task<Result> UpdateEmployee(Guid id, CreateEmployeeRequest request);
   
   Task<Result> AssignEmployee(Guid id, AssignEmployeeDto employeeDto);
   
   Task<Result> DeleteEmployee(Guid id, Guid userId);
}