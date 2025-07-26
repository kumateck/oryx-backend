using APP.Utils;
using DOMAIN.Entities.Employees;
using SHARED;
using SHARED.Requests;

namespace APP.IRepository;

public interface IEmployeeRepository
{
   Task<Result> OnboardEmployees(OnboardEmployeeDto employeeDto);
   
   Task<Result<Guid>> CreateEmployee(CreateEmployeeRequest request);
   
   Task<Result> CreateEmployeeUser(EmployeeUserDto employeeUserDto);

   Task<Result> UploadAvatar(UploadFileRequest request, Guid employeeId);
   Task<Result<Paginateable<IEnumerable<EmployeeDto>>>> GetEmployees(int page, int pageSize,
      string searchQuery = null, string designation = null, string department = null, EmployeeStatus? status = EmployeeStatus.Active);
   
   Task<Result<IEnumerable<EmployeeDto>>> GetEmployeesByDepartment(Guid departmentId);
   
   Task<Result<IEnumerable<MinimalEmployeeInfoDto>>> GetAvailableEmployeesByDepartment(Guid shiftScheduleId, DateTime date);
   Task<Result<EmployeeDto>> GetEmployee(Guid id);
   Task<Result> UpdateEmployee(Guid id, UpdateEmployeeRequest request);
   
   Task<Result> AssignEmployee(Guid id, AssignEmployeeDto employeeDto);
   
   Task<Result> DeleteEmployee(Guid id, Guid userId);
}