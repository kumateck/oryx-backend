using DOMAIN.Entities.Employees;
using DOMAIN.Entities.ShiftTypes;

namespace DOMAIN.Entities.ShiftAssignments;

public class ShiftAssignmentDto 
{
    public List<MinimalEmployeeInfoDto> Employees { get; set; }
    public DateTime ScheduleDate { get; set; }
    
    public ShiftCategoryDto ShiftCategory { get; set; }
    
    public MinimalShiftTypeDto ShiftType { get; set; }
    
    public MinimalShiftScheduleDto ShiftSchedule { get; set; }
}

public class MinimalShiftScheduleDto
{
    public Guid ScheduleId { get; set; }
    
    public string ScheduleName { get; set; }
}
