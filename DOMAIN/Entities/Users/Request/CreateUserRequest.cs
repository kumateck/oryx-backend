using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.Users.Request;

public class CreateUserRequest
{
    [Required] public string FirstName { get; set; }
    [Required] public string LastName { get; set; }
    [Required]public string UserName { get; set; }
    [Required][EmailAddress]
    [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", ErrorMessage = "Invalid email address.")]
    public string Email { get; set; }
    public DateTime? DateOfBirth { get; set; }
    [Required] public string Password { get; set; }
    [Phone] public string PhoneNumber { get; set; }
    public Guid? DepartmentId { get; set; }
    public string Avatar { get; set; }
    public List<string> RoleNames { get; } = [];
}