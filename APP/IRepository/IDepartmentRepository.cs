using APP.Utils;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Departments.Request;
using SHARED;

namespace APP.IRepository;

public interface IDepartmentRepository
{
    Task<Result<Guid>> CreateDepartment(CreateDepartmentRequest request, Guid userId);

    Task<Result<DepartmentDto>> GetDepartment(Guid departmentId);

    Task<Result<Paginateable<IEnumerable<DepartmentDto>>>> GetDepartments(int page, int pageSize, string searchQuery);

    Task<Result> UpdateDepartment(CreateDepartmentRequest request, Guid departmentId, Guid userId);

    Task<Result> DeleteDepartment(Guid departmentId, Guid userId);
}
