using SHARED;

namespace DOMAIN.Entities.Roles;

public class RoleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public DepartmentType Type { get; set; }
    public string DisplayName { get; set; }
    public bool IsManager { get; set; }
}