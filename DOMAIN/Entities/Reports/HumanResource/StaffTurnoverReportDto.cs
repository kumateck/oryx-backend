namespace DOMAIN.Entities.Reports.HumanResource;

public class StaffTurnoverReportDto
{
    public List<StaffTurnoverCountDto> Departments = [];
}

public class StaffTurnoverCountDto
{
    public string DepartmentName { get; set; }
}