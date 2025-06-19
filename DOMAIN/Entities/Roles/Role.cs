using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;
using Microsoft.AspNetCore.Identity;

namespace DOMAIN.Entities.Roles;

public class Role : IdentityRole<Guid>, IBaseEntity
{
    [StringLength(100)] public string DisplayName { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedById { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? LastUpdatedById { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? LastDeletedById { get; set; }
    public DepartmentType Type { get; set; }
    public bool IsManager { get; set; }
}

public class RoleDepartment
{
    public Guid Id { get; set; }
    public Guid RoleId { get; set; }
    public Role Role { get; set; }
    public Guid DepartmentId { get; set; }
    public Department Department { get; set; }
}