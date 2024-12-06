using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Roles;
using Microsoft.AspNetCore.Identity;

namespace DOMAIN.Entities.Users;

public class UserDto
{
    public Guid Id { get; set; }
    [PersonalData] public string FirstName { get; set; }
    [PersonalData] public string LastName { get; set; }
    [EmailAddress] public string Email { get; set; }
    public string Avatar { get; set; }
    public DepartmentDto Department { get; set; }
    //public List<RoleDto> Roles { get; set; }
}