using DOMAIN.Entities.Base;
using DOMAIN.Entities.Employees;

namespace DOMAIN.Entities.ExitPassRequests;

public class ExitPassRequestDto: BaseDto
{
    public DateTime Date { get; set; }
    
    public TimeOnly TimeIn { get; set; }
    
    public TimeOnly TimeOut { get; set; }
    
    public string Justification { get; set; }
    
    public Guid EmployeeId { get; set; }
    
    public Employee Employee { get; set; }
}