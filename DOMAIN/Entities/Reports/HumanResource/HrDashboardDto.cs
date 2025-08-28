namespace DOMAIN.Entities.Reports.HumanResource;

public class HrDashboardDto
{
    public int NumberOfLeaveRequests { get; set; }
    public int NumberOfPendingLeaveRequests { get; set; }
    public int NumberOfRejectedLeaveRequests { get; set; }
    public int NumberOfExpiredLeaveRequests { get; set; }
    public int NumberOfOvertimeRequests { get; set; }
    public int NumberOfPendingOvertimeRequests { get; set; }
    public int NumberOfApprovedOvertimeRequests { get; set; }
    public int NumberOfExpiredOvertimeRequests { get; set; }
    
    public int NumberOfAbsenceRequests { get; set; }
    public int NumberOfPendingAbsenceRequests { get; set; }
    public int NumberOfApprovedAbsenceRequests { get; set; }
    public int NumberOfRejectedAbsenceRequests { get; set; }
    
    public int NumberOfExitPasses { get; set; }
    public int NumberOfPendingExitPasses { get; set; }
    public int NumberOfApprovedExitPasses { get; set; }
    public int NumberOfRejectedExitPasses { get; set; }
    
    public int NumberOfOfficialDutyLeaves { get; set; }
    public int NumberOfPendingOfficialDutyLeaves { get; set; }
    public int NumberOfApprovedOfficialDutyLeaves { get; set; }
    public int NumberOfRejectedOfficialDutyLeaves { get; set; }
    
    public int NumberOfStaffRequisitions { get; set; }
    public int TotalEmployees => NumberOfCasualEmployees + NumberOfPermanentEmployees; 
    
    public int NumberOfNewCasualEmployees { get; set; }
    public int NumberOfNewPermanentEmployees { get; set; }
    public int NumberOfNewEmployees => NumberOfNewCasualEmployees + NumberOfNewPermanentEmployees;
    
    public int NumberOfActiveCasualEmployees { get; set; }
    public int NumberOfActivePermanentEmployees { get; set; }
    public int NumberOfActiveEmployees => NumberOfActiveCasualEmployees + NumberOfActivePermanentEmployees;
    
    public int NumberOfInactiveCasualEmployees { get; set; }
    public int NumberOfInactivePermanentEmployees { get; set; }
    
    public int NumberOfInactiveEmployees => NumberOfInactiveCasualEmployees + NumberOfInactivePermanentEmployees;
    public int NumberOfCasualEmployees { get; set; }
    public int NumberOfPermanentEmployees { get; set; }
    
    public AttendanceStatsDto AttendanceStats { get; set; }
    
    public decimal EmployeeGenderRatio { get; set; }
}

public class AttendanceStatsDto
{
    public int NumberOfAbsentEmployees { get; set; }
    public int NumberOfPresentEmployees { get; set; }
    public int AttendanceRate { get; set; }
}