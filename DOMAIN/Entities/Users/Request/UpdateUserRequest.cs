using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.Users.Request;

public class UpdateUserRequest 
{
    [Required] public string FirstName { get; set; }
    [Required] public string LastName { get; set; }
    [Phone] public string PhoneNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public Guid? DepartmentId { get; set; }
    public string Avatar { get; set; }
    public List<string> RoleNames { get; } = [];
}