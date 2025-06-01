using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Employees;
using DOMAIN.Entities.LeaveTypes;

namespace DOMAIN.Entities.LeaveRequests;

public class LeaveRequest : BaseEntity, IRequireApproval
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    
    public string ContactPerson { get; set; }
    
    public string ContactPersonNumber { get; set; }

    public string Justification { get; set; }
    
    public DateTime RecallDate { get; set; }
    
    public string RecallReason { get; set; }

    public RequestCategory RequestCategory { get; set; }
    public LeaveStatus LeaveStatus { get; set; }
    public int? UnpaidDays { get; set; }
    
    public int? PaidDays { get; set; }
    public Guid EmployeeId { get; set; }
    
    public Employee Employee { get; set; }
    
    public Guid LeaveTypeId { get; set; }
    public LeaveType LeaveType { get; set; }

    public List<LeaveRequestApproval> Approvals { get; set; } = [];
    
    public bool Approved { get; set; }
}

public class LeaveRequestApproval: ResponsibleApprovalStage
{
    public Guid Id { get; set; }
    
    public Guid LeaveRequestId { get; set; }
    
    public LeaveRequest LeaveRequest { get; set; }
    
    public Guid ApprovalId { get; set; }
    
    public Approval Approval { get; set; }
}

public enum RequestCategory
{
    LeaveRequest,
    AbsenceRequest,
    ExitPassRequest
}

public enum LeaveStatus
{
    Pending,
    Approved,
    Rejected,
    Expired
}

