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

public class HrKpiFilterDto
{
    public Guid? DepartmentId { get; set; }
    public EmployeeType? EmployeeType { get; set; }
    public EmployeeStatus? Status { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
