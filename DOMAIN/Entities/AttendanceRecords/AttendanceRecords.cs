using DOMAIN.Entities.Base;


namespace DOMAIN.Entities.AttendanceRecords;

public class AttendanceRecords : BaseEntity
{
    public string EmployeeId { get; set; }
    
    public DateTime TimeStamp { get; set; }
    
    public WorkState WorkState { get; set; }
}

public enum WorkState
{
    CheckIn,
    CheckOut,
}
