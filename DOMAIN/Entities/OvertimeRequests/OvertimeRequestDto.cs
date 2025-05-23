using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Employees;

namespace DOMAIN.Entities.OvertimeRequests;

public class OvertimeRequestDto : BaseDto
{
    public string Code { get; set; }
    
    public List<EmployeeDto> Employees { get; set; }
    
    public DateTime OvertimeDate { get; set; }
    
    public string StartTime { get; set; }
    
    public string EndTime { get; set; }
    
    public OvertimeStatus Status { get; set; }
    
    public string Justification { get; set; }
    
    public Guid DepartmentId { get; set; }
    
    public Department Department { get; set; }
    
    public int TotalHours { get; set; }
}