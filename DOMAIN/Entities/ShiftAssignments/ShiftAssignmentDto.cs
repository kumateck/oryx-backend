using DOMAIN.Entities.Employees;
using DOMAIN.Entities.ShiftSchedules;

namespace DOMAIN.Entities.ShiftAssignments;

public class ShiftAssignmentDto
{
    public Guid EmployeeId { get; set; }
    
    public Employee Employee { get; set; }
    
    public Guid ShiftScheduleId { get; set; }
    
    public DateTime ScheduleDate { get; set; }
    
    public Guid ShiftCategoryId { get; set; }
    
    public ShiftCategory ShiftCategory { get; set; }
    
    public ShiftSchedule ShiftSchedules { get; set; }
}