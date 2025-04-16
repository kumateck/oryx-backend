using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Roles;
using Microsoft.AspNetCore.Identity;
using SHARED;

namespace DOMAIN.Entities.Users;

public class UserDto
{
    public Guid Id { get; set; }
    [PersonalData] public string FirstName { get; set; }
    [PersonalData] public string LastName { get; set; }
    [EmailAddress] public string Email { get; set; }
    public string Avatar { get; set; }
    public string Signature { get; set; }
    public CollectionItemDto Department { get; set; }
}

public class UserWithRoleDto : UserDto
{ 
    public List<RoleDto> Roles { get; set; } = [];
}