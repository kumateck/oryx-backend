using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.LeaveRecalls;

public class CreateLeaveRecallRequest
{
    [Required] public Guid EmployeeId { get; set; }
    
    [Required] public DateTime RecallDate { get; set; }
    
    public string Reason { get; set; }
    
}