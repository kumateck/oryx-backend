using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.Approvals;

public class DelegateApproval
{
    [Required] public DateTime StartDate { get; set; }
    
    [Required] public DateTime EndDate { get; set; }
    
    [Required] public Guid EmployeeId { get; set; }
}