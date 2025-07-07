namespace DOMAIN.Entities.Reports;

public class HumanResourceReportDto
{
    public int NumberOfLeaveRequests { get; set; }
    public int NumberOfPendingLeaveRequests { get; set; }
    public int NumberOfRejectedLeaveRequests { get; set; }
    public int NumberOfExpiredLeaveRequests { get; set; }
    public int NumberOfOvertimeRequests { get; set; }
    public int NumberOfPendingOvertimeRequests { get; set; }
    public int NumberOfApprovedOvertimeRequests { get; set; }
    public int NumberOfExpiredOvertimeRequests { get; set; }
    public int TotalEmployees => NumberOfCasualEmployees + NumberOfPermanentEmployees;
    public int NumberOfCasualEmployees { get; set; }
    public int NumberOfPermanentEmployees { get; set; }
    
    public AttendanceStatsDto AttendanceStats { get; set; }
}

public class AttendanceStatsDto
{
    public int NumberOfAbsentEmployees { get; set; }
    public int NumberOfPresentEmployees { get; set; }
    public int AttendanceRate { get; set; }
}

public class PermanentStaffGradeCountDto
{
    public int No { get; set; }
    public string Department { get; set; }
    public int SeniorMgtMale { get; set; }
    public int SeniorMgtFemale { get; set; }
    public int SeniorStaffMale { get; set; }
    public int SeniorStaffFemale { get; set; }
    public int JuniorStaffMale { get; set; }
    public int JuniorStaffFemale { get; set; }
}