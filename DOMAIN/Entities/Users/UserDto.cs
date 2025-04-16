using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using SHARED;

namespace DOMAIN.Entities.Users;

public class UserDto
{
    public Guid Id { get; set; }
    [PersonalData] public string FirstName { get; set; }
    [PersonalData] public string LastName { get; set; }
    [EmailAddress] public string Email { get; set; }
    
    public bool IsDisabled { get; set; }
    
    public string Role { get; set; }
    public string Avatar { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public string Signature { get; set; }
    public CollectionItemDto Department { get; set; }
}