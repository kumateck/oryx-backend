using DOMAIN.Entities.Employees;

namespace DOMAIN.Entities.Approvals;

public class DelegateApprovalDto
{
    public DateTime StartDate { get; set; }
    
    public DateTime EndDate { get; set; }
    
    public Guid EmployeeId { get; set; }
    
    public EmployeeDto Employee { get; set; }
}