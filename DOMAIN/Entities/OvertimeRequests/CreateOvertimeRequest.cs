using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.OvertimeRequests;

public class CreateOvertimeRequest
{
    [Required] public List<Guid> EmployeeIds { get; set; }
    
    [Required] public DateTime OvertimeDate { get; set; }
    
    [Required] public DateTime StartDate { get; set; }
    [Required] public Guid DepartmentId { get; set; }
    [Required, StringLength(10)] public string StartTime { get; set; }
    
    [Required] public DateTime EndDate { get; set; }
    
    [Required, StringLength(10)] public string EndTime { get; set; }
    
    public string Justification { get; set; }
    
   
}