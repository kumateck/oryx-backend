using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;
using Microsoft.AspNetCore.Identity;

namespace DOMAIN.Entities.Users;

public class User : IdentityUser<Guid>, IBaseEntity 
{
    [PersonalData][StringLength(100)] public string FirstName { get; set; }
    [PersonalData][StringLength(100)] public string LastName { get; set; }
    [StringLength(100)] public string Title { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? LastUpdatedById { get; set; }
    public Guid? CreatedById { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? LastDeletedById { get; set; }
    [StringLength(100)] public string Avatar { get; set; }
    public bool IsDisabled { get; set; }
    public Guid? DepartmentId { get; set; }
    public Department Department { get; set; }
}

public class UserDepartment
{
    public Guid UserId { get; set; }
    public User User { get; set; }
    public Guid DepartmentId { get; set; }
    public Department Department { get; set; }
}