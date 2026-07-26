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

public class HrKpiFilterDto
{
    public Guid? DepartmentId { get; set; }
    public EmployeeType? EmployeeType { get; set; }
    public EmployeeStatus? Status { get; set; }
}
