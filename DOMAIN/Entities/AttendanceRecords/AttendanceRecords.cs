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

public static class WorkStateExtensions
{
    public static string ToDisplayString(this WorkState state)
    {
        return state switch
        {
            WorkState.CheckIn => "Check In",
            WorkState.CheckOut => "Check Out",
            _ => state.ToString()
        };
    }
}