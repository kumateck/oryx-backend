namespace DOMAIN.Entities.LeaveRequests;

public class ReapplyLeaveRequest
{
    public Guid LeaveRequestId { get; set; }
    public DateTime NewStartDate { get; set; }
    public DateTime NewEndDate { get; set; }
    public string Justification { get; set; }
}