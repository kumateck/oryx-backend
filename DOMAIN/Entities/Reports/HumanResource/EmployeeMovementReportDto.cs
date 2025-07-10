namespace DOMAIN.Entities.Reports.HumanResource;

public class EmployeeMovementReportDto
{
    public string DepartmentName { get; set; }
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
}