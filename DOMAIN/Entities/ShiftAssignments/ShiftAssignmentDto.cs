using DOMAIN.Entities.Employees;

namespace DOMAIN.Entities.ShiftAssignments;

public class ShiftAssignmentDto 
{
    public List<MinimalEmployeeInfoDto> Employees { get; set; }
    
    public DateTime ScheduleDate { get; set; }
    
    public Guid ShiftCategoryId { get; set; }
    
    public Guid ShiftTypeId { get; set; }
    
    public string ShiftTypeName { get; set; }
    
    public string ShiftCategoryName { get; set; }
    
    public string ShiftScheduleName { get; set; }
}
