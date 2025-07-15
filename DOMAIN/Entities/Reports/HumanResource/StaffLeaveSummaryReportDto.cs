namespace DOMAIN.Entities.Reports.HumanResource;

public class StaffLeaveSummaryReportDto
{
    public List<StaffLeaveSummaryDto> Departments { get; set; } = [];
}

public class StaffLeaveSummaryDto
{
    public string DepartmentName { get; set; }
    public int StaffDueForLeave { get; set; }
    public int TotalLeaveEntitlement { get; set; }
    public int DaysUsed { get; set; }
    public int DaysLeft => TotalLeaveEntitlement - DaysUsed;
    public double PercentUsed => (double)DaysUsed / TotalLeaveEntitlement * 100;
    public double PercentLeft => 100 - PercentUsed;
}