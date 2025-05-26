using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.ShiftAssignments;

public class AssignShiftRequest
{
    [Required] public List<Guid> EmployeeIds { get; set; }
    
    [Required] public Guid ShiftScheduleId { get; set; }
    
    [Required, StringLength(100)] public string ShiftName { get; set; }
}
