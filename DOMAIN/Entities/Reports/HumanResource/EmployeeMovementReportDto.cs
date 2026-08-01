namespace DOMAIN.Entities.Reports.HumanResource;

public class EmployeeMovementReportDto
{
    public List<EmployeeMovementCountDto> Departments { get; set; } = [];

    public EmployeeMovementGrandTotalDto Totals { get; set; }
}

public class EmployeeMovementGrandTotalDto
{
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

    // Net Movement = (Total New Hires) - (Total Leavers)
    public int NetMovement =>
        (PermanentNew + CasualNew)
        - (
            PermanentResignation
            + PermanentTermination
            + PermanentSDVP
            + CasualResignation
            + CasualTermination
            + CasualSDVP
        );
}

public class EmployeeMovementCountDto : EmployeeMovementGrandTotalDto
{
    public string DepartmentName { get; set; }
}