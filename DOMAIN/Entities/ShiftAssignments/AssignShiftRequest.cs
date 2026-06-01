using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.ShiftAssignments;

public class AssignShiftRequest
{
    [Required] public List<Guid> EmployeeIds { get; set; }

    [Required] public Guid ShiftScheduleId { get; set; }

    [Required] public Guid ShiftCategoryId { get; set; }

    [Required] public Guid ShiftTypeId { get; set; }
}

public class SwapShiftRequest
{
    public Guid EmployeeId { get; set; }
    
    public Guid NewEmployeeId { get; set; }

    public Guid ShiftScheduleId { get; set; }

    public Guid ShiftCategoryId { get; set; }

    public Guid ShiftTypeId { get; set; }
    public DateTime ScheduleDate { get; set; }
}
