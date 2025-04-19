using DOMAIN.Entities.Base;
using DOMAIN.Entities.LeaveTypes;

namespace DOMAIN.Entities.LeaveRequests;

public class LeaveRequest : BaseEntity
{
    public Guid LeaveTypeId { get; set; }
    
    public LeaveType LeaveType { get; set; }
}