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
    
    // Total calculations - net movement for the period
    public int TotalPermanent => PermanentNew - PermanentTransfer - PermanentResignation - PermanentTermination - PermanentSDVP;
    public int TotalCasual => CasualNew - CasualResignation - CasualTermination - CasualSDVP;
    public int GrandTotal => TotalPermanent + TotalCasual;
}

public class EmployeeMovementCountDto : EmployeeMovementGrandTotalDto
{
    public string DepartmentName { get; set; }
}