namespace DOMAIN.Entities.AttendanceRecords;

public class AttendanceRecordDepartmentDto
{
    public string StaffName { get; set; }
    
    public string EmployeeId { get; set; }
    
    public string ShiftName { get; set; }
    public string ClockInTime { get; set; }
    public string ClockOutTime { get; set; }
    public double WorkHours { get; set; }
       
}