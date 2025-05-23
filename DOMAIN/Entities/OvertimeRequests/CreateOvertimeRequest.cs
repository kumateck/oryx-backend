using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.OvertimeRequests;

public class CreateOvertimeRequest
{
    [Required] public string Code {get; set;}
    [Required] public List<Guid> EmployeeIds { get; set; }
    
    [Required] public DateTime OvertimeDate { get; set; }
    [Required] public Guid DepartmentId { get; set; }
    [Required, StringLength(10)] public string StartTime { get; set; }
    
    [Required, StringLength(10)] public string EndTime { get; set; }
    
    public string Justification { get; set; }
    
   
}