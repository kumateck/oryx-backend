using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;

namespace DOMAIN.Entities.OvertimeRequests;

public class OvertimeRequest: BaseEntity
{
    public List<Guid> EmployeeIds { get; set; }
    public DateTime OvertimeDate { get; set; }
    
    public DateTime StartDate { get; set; }
    public string StartTime { get; set; }
    
    public DateTime EndDate { get; set; }
    
    public string EndTime { get; set; }
    
    public OvertimeStatus Status { get; set; } = OvertimeStatus.Pending;
    
    public string Justification { get; set; }
    
    public Guid DepartmentId { get; set; }
    
    public Department Department { get; set; }
    
    
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

public enum OvertimeStatus
{
    Pending,
    Approved,
    Rejected,
    Expired
}
