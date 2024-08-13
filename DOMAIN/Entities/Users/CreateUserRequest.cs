using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.Users;

public class CreateUserRequest
{
    public string Title { get; set; }
    [Required] public string FirstName { get; set; }
    [Required] public string LastName { get; set; }
    [Required]public string UserName { get; set; }
    [Required][EmailAddress] public string Email { get; set; }
    [Required] public string Password { get; set; }
    [Phone] public string PhoneNumber { get; set; }
    public string Sex { get; set; }
    public DateTime HiredOn { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public Guid DirectReportId { get; set; }
    public string Avatar { get; set; }
    public List<string> RoleNames { get; } = [];
}