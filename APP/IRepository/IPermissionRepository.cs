using APP.Utils;
using SHARED;

namespace APP.IRepository;

public interface IPermissionRepository
{ 
    IEnumerable<PermissionModuleDto> GetAllPermissionInSystem();
    IEnumerable<PermissionDto> GetAllPermissions();
    Task<Result<List<PermissionModuleDto>>> GetAllPermissionForUser(Guid userId);
    Task<List<PermissionModuleDto>> GetPermissionByRole(Guid roleId);
    Task<Result> UpdateRolePermissions(List<string> permissions, Guid roleId);
}