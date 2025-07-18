
namespace DOMAIN.Entities.AttendanceRecords;

public class GeneralAttendanceReportDto
{
    public string DepartmentName { get; set; }
    
    public int PermanentStaff { get; set; }
    
    public int CasualStaff { get; set; }
    
    public int TotalStaff => PermanentStaff + CasualStaff;
    
    public int PermanentMorning { get; set; }
    
    public int PermanentAfternoon { get; set; }
    
    public int PermanentNight { get; set; }
    
    public int CasualMorning { get; set; }
    
    public int CasualAfternoon { get; set; }
    
    public int CasualNight { get; set; }
    
    public int ApprovedLeaves { get; set; }
    
    public int Absences { get; set; }
    
    public int Suspensions { get; set; }
    
    public int SickLeaves {get; set;}
    
    public int MaternityLeaves {get; set;}
    
}