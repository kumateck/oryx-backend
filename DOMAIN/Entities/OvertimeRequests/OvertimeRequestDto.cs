using DOMAIN.Entities.Employees;

namespace DOMAIN.Entities.OvertimeRequests;

public class OvertimeRequestDto
{
    public List<EmployeeDto> Employees { get; set; }
    
    public DateTime OvertimeDate { get; set; }
    
    public DateTime StartDate { get; set; }
    
    public DateTime EndDate { get; set; }
    
    public string Justification { get; set; }
    
    public int TotalHours { get; set; }
}