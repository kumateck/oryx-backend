using DOMAIN.Entities.Employees;
using DOMAIN.Entities.ShiftSchedules;
using DOMAIN.Entities.ShiftTypes;

namespace DOMAIN.Entities.ShiftAssignments;

public class ShiftAssignmentDto
{
    public Guid EmployeeId { get; set; }
    
    public Employee Employee { get; set; }
    
    public Guid ShiftScheduleId { get; set; }
    
    public DateTime ScheduleDate { get; set; }
    
    public Guid ShiftCategoryId { get; set; }
    
    public Guid ShiftTypeId { get; set; }
    
    public ShiftCategory ShiftCategory { get; set; }
    
    public ShiftSchedule ShiftSchedules { get; set; }
    
    public ShiftType ShiftType { get; set; }
    
    public List<EmployeeDto> Employees { get; set; } 
}