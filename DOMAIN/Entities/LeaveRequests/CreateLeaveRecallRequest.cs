using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.LeaveRequests;

public class CreateLeaveRecallRequest
{
    
    [Required] public Guid EmployeeId { get; set; }
    [Required] public DateTime RecallDate { get; set; }
    
    public string RecallReason { get; set; }
}