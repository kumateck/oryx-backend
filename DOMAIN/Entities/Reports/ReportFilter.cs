namespace DOMAIN.Entities.Reports;

public class ReportFilter
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class MovementReportFilter : ReportFilter
{
    public Guid? DepartmentId { get; set; }
}