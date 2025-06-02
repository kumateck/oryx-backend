using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.ShiftAssignments;

public class UpdateShiftAssignment
{
    [Required] public Guid ShiftCategoryId { get; set; }
    
    [Required] public DateTime ScheduleDate { get; set; }

    public List<Guid> AddEmployeeIds { get; set; } = [];
    public List<Guid> RemoveEmployeeIds { get; set; } = [];
}