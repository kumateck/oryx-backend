using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.LeaveEntitlements;

public class LeaveEntitlementDto
{
    [Required] public Guid EmployeeId { get; set; }
    
    [Required] public int Year { get; set; }
    
    [Range(1, 366, ErrorMessage = "Days allowed must be between 1 and 366.")]
    public int DaysAllowed { get; set; }
}