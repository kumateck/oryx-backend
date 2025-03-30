using DOMAIN.Entities.Employees;
using SHARED;

namespace APP.IRepository;

public interface IEmployeeRepository
{
   Task<Result<Guid?>> CreateEmployee(CreateEmployeeRequest request, Guid userId);
   Task<Result<EmployeeDto>> GetEmployee(Guid id);
   Task<Result> UpdateEmployee(Guid id, CreateEmployeeRequest request, Guid userId);
}