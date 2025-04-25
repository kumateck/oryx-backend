using DOMAIN.Entities.Base;
using DOMAIN.Entities.ShiftTypes;

namespace DOMAIN.Entities.ShiftSchedules;

public class ShiftSchedule: BaseEntity
{
    public string ScheduleName { get; set; }
    
    public List<ShiftType> ShiftTypes { get; set; }
    
    public List<DayOfWeek> DaysOfWeek { get; set; }
}