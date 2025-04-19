using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Permissions;

namespace DOMAIN.Entities.Roles;

public class CreateRoleRequest
{
    [Required] public string Name { get; set; }
    public List<PermissionModuleDto> Permissions { get; set; } = [];
}