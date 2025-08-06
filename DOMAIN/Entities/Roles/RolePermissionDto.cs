using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Permissions;
using SHARED;

namespace DOMAIN.Entities.Roles;

public class RolePermissionDto
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; }
    public string Name { get; set; }
    public DepartmentType Type { get; set; }
    public List<PermissionModuleDto> Permissions { get; set; } = [];
}