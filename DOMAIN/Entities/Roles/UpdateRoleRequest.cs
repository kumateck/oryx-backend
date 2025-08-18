using System.ComponentModel.DataAnnotations;
using SHARED;

namespace DOMAIN.Entities.Roles;

public class UpdateRoleRequest
{
    [Required] public string Name { get; set; }
    [Required] public string DisplayName { get; set; }
    public DepartmentType Type { get; set; }
}