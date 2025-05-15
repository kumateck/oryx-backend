using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.LeaveRecalls;

public class LeaveRecall : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public DateTime RecallDate { get; set; }
    public string Reason { get; set; }
    
}