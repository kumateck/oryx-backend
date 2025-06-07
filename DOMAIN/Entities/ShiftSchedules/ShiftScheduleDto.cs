using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.ShiftTypes;

namespace DOMAIN.Entities.ShiftSchedules;

public class ShiftScheduleDto: BaseDto
{
   public string ScheduleName { get; set; }
   
   public DayOfWeek? StartDate { get; set; }
   
   public ScheduleFrequency Frequency { get; set; }
   public List<ShiftTypeDto> ShiftType { get; set; }
   
   public Guid DepartmentId { get; set; }
   
   public DepartmentDto Department { get; set; }

}
