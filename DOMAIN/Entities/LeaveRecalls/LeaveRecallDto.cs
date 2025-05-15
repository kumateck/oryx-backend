using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.LeaveRecalls;

public class LeaveRecallDto : BaseDto
{
    public Guid EmployeeId { get; set; }
    public DateTime RecallDate { get; set; }
    public string Reason { get; set; }
}