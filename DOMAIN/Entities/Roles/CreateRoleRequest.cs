using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Permissions;

namespace DOMAIN.Entities.Roles;

public class CreateRoleRequest
{
    [Required] public string Name { get; set; }
    public DepartmentType Type { get; set; }
    public List<PermissionModuleDto> Permissions { get; set; } = [];
}