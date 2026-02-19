namespace DOMAIN.Entities.Reports.Requisition;

public class RequisitionReportDto
{
    public int NewRequisitionsCount { get; set; }
    public int PendingRequisitionsCount { get; set; }
    public int RejectedRequisitionsCount { get; set; }
    public int CompletedRequisitionsCount { get; set; }
    public int SourcedRequisitionsCount { get; set; }
   
}

