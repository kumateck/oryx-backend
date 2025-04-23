using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.AbsenceRequests;

public class CreateAbsenceRequest
{
    [Required] public Guid LeaveTypeId { get; set; }
    
    [Required] public Guid EmployeeId { get; set; }
    
    [Required] public DateTime StartDate { get; set; }
    
    [Required] public DateTime EndDate { get; set; }
}