using APP.Utils;
using DOMAIN.Entities.EmployeePayrollProfiles;
using SHARED;

namespace APP.IRepository;

public interface IEmployeePayrollProfileRepository
{
    Task<Result<Guid>> CreateEmployeePayrollProfile(CreateEmployeePayrollProfileRequest request);
    Task<Result<Paginateable<IEnumerable<EmployeePayrollProfileDto>>>> GetEmployeePayrollProfiles(int page, int pageSize, string searchQuery, Guid? payGroupId = null);
    Task<Result<EmployeePayrollProfileDto>> GetEmployeePayrollProfile(Guid id);
    Task<Result<EmployeePayrollProfileDto>> GetEmployeePayrollProfileByEmployee(Guid employeeId);
    Task<Result> UpdateEmployeePayrollProfile(Guid id, CreateEmployeePayrollProfileRequest request);
    Task<Result> DeleteEmployeePayrollProfile(Guid id, Guid userId);
}