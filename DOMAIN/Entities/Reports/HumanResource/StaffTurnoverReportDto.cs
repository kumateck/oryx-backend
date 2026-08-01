namespace DOMAIN.Entities.Reports.HumanResource;

public class StaffTurnoverReportDto
{
    public List<StaffTurnoverCountDto> Departments { get; set; } = [];
    public double OrganisationTurnover { get; set; }
    public int GrandTotalLeavers { get; set; }
}

public class StaffTurnoverCountDto
{
    public int No { get; set; }
    public string Department { get; set; }
    public string ExitReason { get; set; }
    public int LeaverCount { get; set; }
    public int TotalLeavers { get; set; }
    public double AverageHeadcount { get; set; }
    public double DepartmentalTurnover { get; set; }
}

