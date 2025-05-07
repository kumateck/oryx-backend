using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.LeaveRequests;

public class CreateLeaveRequest
{
    [Required] public Guid LeaveTypeId { get; set; }
    [Required] public DateTime StartDate { get; set; }
    [Required] public DateTime EndDate { get; set; }
    [Required] public Guid EmployeeId { get; set; }
    [Required] public RequestCategory RequestCategory { get; set; }
    
    [StringLength(100)] public string ContactPerson { get; set; }
    
    [Phone] public string ContactPersonNumber { get; set; }
    
    public string Justification { get; set; }
}