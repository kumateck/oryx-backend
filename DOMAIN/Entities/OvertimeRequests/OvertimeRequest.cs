using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Employees;

namespace DOMAIN.Entities.OvertimeRequests;

public class OvertimeRequest: BaseEntity
{
    public string Code { get; set; }
    public List<Employee> Employees { get; set; }
    public DateTime OvertimeDate { get; set; }
    public string StartTime { get; set; }
    public string EndTime { get; set; }
    public OvertimeStatus Status { get; set; } = OvertimeStatus.Pending;
    
    public string Justification { get; set; }
    
    public Guid DepartmentId { get; set; }
    
    public Department Department { get; set; }
    public List<OvertimeRequestApproval> Approvals { get; set; } = [];
    public bool Approved { get; set; }
    public ApprovalStatus ApprovalStatus { get; set; }
    
    public int TotalHours
    {
        get
        {
            if (!TimeOnly.TryParse(StartTime, out var start) || !TimeOnly.TryParse(EndTime, out var end))
                return 0; // or throw exception if invalid time strings
            
            var duration = end.ToTimeSpan() - start.ToTimeSpan();
            return (int)duration.TotalHours;

        }
    }
}

public class OvertimeRequestApproval: ResponsibleApprovalStage
{
    public Guid Id { get; set; }
    
    public Guid OvertimeRequestId { get; set; }
    
    public OvertimeRequest LeaveRequest { get; set; }
    
    public Guid ApprovalId { get; set; }
    
    public Approval Approval { get; set; }
}

public enum OvertimeStatus
{
    Pending,
    Approved,
    Rejected,
    Expired
}
