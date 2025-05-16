using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.ShiftSchedules;

public class CreateShiftScheduleRequest
{
    [Required] public string ScheduleName { get; set; }
    
    [Required] public ScheduleFrequency Frequency { get; set; }
    
    [Required] public string StartTime { get; set; }
    
    public DayOfWeek? StartDate { get; set; }

    [Required] public List<Guid> ShiftTypeIds { get; set; } = [];
    
    [Required] public Guid DepartmentId { get; set; }
}