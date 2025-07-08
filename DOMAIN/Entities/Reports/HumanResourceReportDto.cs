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
    public string Department { get; set; }
    public int SeniorMgtMale { get; set; }
    public int SeniorMgtFemale { get; set; }
    public int SeniorStaffMale { get; set; }
    public int SeniorStaffFemale { get; set; }
    public int JuniorStaffMale { get; set; }
    public int JuniorStaffFemale { get; set; }
    public int TotalSeniorMgt => SeniorMgtMale + SeniorMgtFemale;
    public int TotalSeniorStaff => SeniorStaffMale + SeniorStaffFemale;
    public int TotalJuniorStaff => JuniorStaffMale + JuniorStaffFemale;
    public int TotalStaff => TotalSeniorStaff + TotalJuniorStaff + TotalSeniorMgt;
}

public class MovementReportDto
{
    public string DepartmentName { get; set; }

    // Permanent 
    public int PermanentNew { get; set; }
    public int PermanentTransfer { get; set; }
    public int PermanentResignation { get; set; }
    public int PermanentTermination { get; set; }
    public int PermanentSDVP { get; set; }

    // Casual
    public int CasualNew { get; set; }
    public int CasualResignation { get; set; }
    public int CasualTermination { get; set; }
    public int CasualSDVP { get; set; }
}