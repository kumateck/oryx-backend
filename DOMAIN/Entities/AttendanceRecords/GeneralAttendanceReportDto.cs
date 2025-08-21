
namespace DOMAIN.Entities.AttendanceRecords;

public class GeneralAttendanceReportResponse
{
    public List<GeneralAttendanceReportDto> DepartmentReports { get; set; }
    public StaffGenderRatioReport SystemStatistics { get; set; }
}
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
    
    public StaffGenderRatioReport SystemStatistics { get; set; }
}

public class StaffGenderRatioReport
{
    public List<SystemGeneraltaffCountDto> Departments { get; set; }
    public SystemGeneralStats Totals { get; set; }

}

public class SystemGeneralStats
{
    public int NumberOfPermanentLeaves { get; set; }
    public int NumberOfCasualLeaves { get; set; }
    
    public int NumberOfPermanentSickLeaves { get; set; }
    public int NumberOfCasualSickLeaves { get; set; }
    
    public int NumberOfPermanentMaternityLeave { get; set; }
    public int NumberOfCasualMaternityLeave { get; set; }
    
    public int NumberOfPermanentAbsentEmployees { get; set; }
    public int NumberOfCasualAbsentEmployees { get; set; }
    
    public int NumberOfPermanentOfficialDuty { get; set; }
    public int NumberOfCasualOfficialDuty { get; set; }
    
    public int NumberOfPermanentSuspensions { get; set; }
    public int NumberOfCasualSuspensions { get; set; }
}

public class SystemGeneraltaffCountDto : SystemGeneralStats
{
    public string Department { get; set; }
}