using DOMAIN.Entities.Base;
using DOMAIN.Entities.Employees;
using DOMAIN.Entities.LeaveTypes;

namespace DOMAIN.Entities.LeaveRequests;

public class LeaveRequest : BaseEntity
{
    public DateTime StartDate { get; set; }
    
    public DateTime EndDate { get; set; }
    
    public string ContactPerson { get; set; }
    
    public string ContactPersonNumber { get; set; }
    
    public Guid EmployeeId { get; set; }
    
    public Employee Employee { get; set; }
    
    public Guid LeaveTypeId { get; set; }
    
    public LeaveType LeaveType { get; set; }
}