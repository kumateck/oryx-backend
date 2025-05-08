using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.LeaveEntitlements;

public class CreateLeaveEntitlementRequest
{
    [Required] public Guid EmployeeId { get; set; }
    
    [Required]
    [Range(2024, 2100, ErrorMessage = "Year must be the current year or a future year.")]
    public int Year { get; set; }
    
    [Required] [Range(1, 365)] public int DaysAllowed { get; set; }
}