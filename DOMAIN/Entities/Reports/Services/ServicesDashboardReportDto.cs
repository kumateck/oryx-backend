namespace DOMAIN.Entities.Reports.Services;

public class ServicesDashboardReportDto
{
    public int TotalServiceCount { get; set; }
    public int TotalContractorsCount { get; set; }
    public JobRequisitionCountDto JobRequisitionCount { get; set; }
}

public class JobRequisitionCountDto
{
    public int Pending { get; set; }
    public int Acknowledged { get; set; }
    public int Assigned { get; set; }
    public int JobStarted { get; set; }
    public int Completed { get; set; }
    public int SentToExternal { get; set; }
    public int QuotationReceived { get; set; }
    public int ContractorSelected { get; set; }
    public int Approved { get; set; }
    public int Cancelled { get; set; }
}