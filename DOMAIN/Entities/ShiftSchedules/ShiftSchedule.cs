using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.ShiftTypes;

namespace DOMAIN.Entities.ShiftSchedules;

public class ShiftSchedule: BaseEntity
{
    public string ScheduleName { get; set; }
    
    public ScheduleFrequency Frequency { get; set; }
    
    public TimeOnly StartTime { get; set; }
    
    public DayOfWeek? StartDate { get; set; }
    public List<ShiftType> ShiftTypes { get; set; }
    
    public Guid DepartmentId { get; set; }
    public Department Department { get; set; }

}

public enum ScheduleFrequency
{
    Daily,
    Weekly,
    Biweekly,
    Monthly,
    Yearly
}