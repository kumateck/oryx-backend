using DOMAIN.Entities.Employees;

namespace DOMAIN.Entities.Reports.HrDashboardKpi;

public class EmployeeHeadcountSnapshotDto
{
    public string Department { get; set; }
    public int PermanentActive { get; set; }
    public int CasualActive { get; set; }
    public int TotalActive => PermanentActive + CasualActive;
    public int PermanentInactive { get; set; }
    public int CasualInactive { get; set; }
    public int TotalInactive => PermanentInactive + CasualInactive;
    public int GrandTotal => TotalActive + TotalInactive;
}

public class EmployeeGenderRatioDto
{
    public string Department { get; set; }
    public int PermanentMale { get; set; }
    public int PermanentFemale { get; set; }
    public int CasualMale { get; set; }
    public int CasualFemale { get; set; }
    public int TotalMale => PermanentMale + CasualMale;
    public int TotalFemale => PermanentFemale + CasualFemale;
    public string GenderRatio { get; set; }
}

public class LeaveRequestPipelineDto
{
    public string Department { get; set; }
    public string Category { get; set; }
    public int Pending { get; set; }
    public int Approved { get; set; }
    public int Rejected { get; set; }
    public int Expired { get; set; }
    public int Total => Pending + Approved + Rejected + Expired;
}

public class OvertimeRequestActivityDto
{
    public string Department { get; set; }
    public string Status { get; set; }
    public int Count { get; set; }
    public int TotalHoursRequested { get; set; }
    public int ApprovedHours { get; set; }
}

public class DailyAttendanceRateDto
{
    public string Department { get; set; }
    public int Present { get; set; }
    public int Absent { get; set; }
    public int ExpectedTotal { get; set; }
    public decimal AttendanceRatePercent { get; set; }
}

public class StaffRequisitionPipelineDto
{
    public string Department { get; set; }
    public string Status { get; set; }
    public int Count { get; set; }
    public int TotalPositionsRequired { get; set; }
}

public class EmployeeGradeLevelDistributionDto
{
    public string Department { get; set; }
    public int JuniorStaff { get; set; }
    public int SeniorStaff { get; set; }
    public int SeniorManagement { get; set; }
    public int Total => JuniorStaff + SeniorStaff + SeniorManagement;
}

public class NewHiresThisPeriodDto
{
    public string Department { get; set; }
    public int PermanentNewHires { get; set; }
    public int CasualNewHires { get; set; }
    public int TotalNewHires => PermanentNewHires + CasualNewHires;
}

public class EmployeeTurnoverRateDto
{
    public string Department { get; set; }
    public int Leavers { get; set; }
    public decimal AverageHeadcount { get; set; }
    public decimal TurnoverRatePercent { get; set; }
}

public class LeaveUtilisationRateDto
{
    public string Department { get; set; }
    public int StaffDueForLeave { get; set; }
    public int TotalDaysAllowed { get; set; }
    public int TotalDaysUsed { get; set; }
    public decimal UtilisationPercent { get; set; }
}

public class ActiveDisciplinaryActionsDto
{
    public string Department { get; set; }
    public int UnderQuestion { get; set; }
    public int FormalWarning { get; set; }
    public int FinalWarning { get; set; }
    public int Suspended { get; set; }
    public int TotalUnderDisciplinaryAction => UnderQuestion + FormalWarning + FinalWarning + Suspended;
}

public class HrKpiFilterDto
{
    public Guid? DepartmentId { get; set; }
    public EmployeeType? EmployeeType { get; set; }
    public EmployeeStatus? Status { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? Year { get; set; }
}
