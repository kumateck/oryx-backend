namespace DOMAIN.Entities.Reports.HumanResource;

public class StaffTurnoverReportDto
{
    public double TurnoverRate { get; set; }
    public int GrandTotalLeavers { get; set; }
    public List<StaffTurnoverCountDto> DepartmentSummaries { get; set; } = [];
}

public class StaffTurnoverCountDto
{
    public string DepartmentName { get; set; }
    public Dictionary<string, int> ExitReasons { get; set; } = new();
    public int TotalLeavers => ExitReasons.Values.Sum();
}

