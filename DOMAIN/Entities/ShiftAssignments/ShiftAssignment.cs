using DOMAIN.Entities.Base;
using DOMAIN.Entities.Employees;
using DOMAIN.Entities.ShiftSchedules;
using DOMAIN.Entities.ShiftTypes;

namespace DOMAIN.Entities.ShiftAssignments;

public class ShiftAssignment : BaseEntity
{
    public DateTime ScheduleDate { get; set; }
    public Guid EmployeeId { get; set; }
    
    public Employee Employee { get; set; }
    
    public Guid ShiftScheduleId { get; set; }
    
    public Guid ShiftTypeId {get; set;}
    
    public Guid ShiftCategoryId { get; set; }
    
    public ShiftCategory ShiftCategory { get; set; }
    
    public ShiftType ShiftType { get; set; }
    
    public ShiftSchedule ShiftSchedules { get; set; }
}

public class ShiftCategory : BaseEntity
{
    public string Name { get; set; }
}

public class ShiftCategoryDto
{
    public Guid Id { get; set; }
    
    public string Name { get; set; }
}