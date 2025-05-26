using DOMAIN.Entities.Base;
using DOMAIN.Entities.Employees;
using DOMAIN.Entities.ShiftSchedules;

namespace DOMAIN.Entities.ShiftAssignments;

public class ShiftAssignment : BaseEntity
{
    public Guid EmployeeId { get; set; }
    
    public Employee Employee { get; set; }
    
    public Guid ShiftScheduleId { get; set; }
    
    public Guid ShiftCategoryId { get; set; }
    
    public ShiftCategory ShiftCategory { get; set; }
    
    public ShiftSchedule ShiftSchedules { get; set; }
}

public class ShiftCategory : BaseEntity
{
    public string Name { get; set; }
}