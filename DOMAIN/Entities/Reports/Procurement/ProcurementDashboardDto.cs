using DOMAIN.Entities.PurchaseOrders;

namespace DOMAIN.Entities.Reports.Procurement;

public class ProcurementDashboardDto
{
    public RequisitionReportDto RequisitionReport { get; set; }
    public PurchaseOrderStatusReportDto  PurchaseOrderStatus { get; set; }
    public SupplierQuotationItemStatusReportDto SalesQuotation { get; set; }
    public MaterialDistributionStatusCountDto MaterialDistributionStatus { get; set; }
    
}

public class PurchaseOrderStatusReportDto
{
    public int NewCount { get; set; }
    public int PendingCount { get; set; }
    public int DeliveredCount { get; set; }
    public int AttachedCount { get; set; }
    public int PendingCheckCount { get; set; }
    public int CheckedCount { get; set; }
    public int ApprovedCount { get; set; }
    public int CompletedCount { get; set; }
    public int PartiallyLinkedCount { get; set; }
    public int LinkedCount { get; set; }
    public int RevisedCount { get; set; }
    public int CancelledCount { get; set; }
}

public class MaterialDistributionStatusCountDto
{ public int  PendingCount { get; set; }
  public int  DistributedCount { get; set; }
    
}
public class SupplierQuotationStatusCountDto
{
    public int NotProcessedCount { get; set; }
    public int ProcessedCount { get; set; }
    public int NotUsedCount { get; set; }
}

public class SupplierQuotationItemStatusReportDto
{
    public SupplierQuotationStatusCountDto Local { get; set; } = new();
    public SupplierQuotationStatusCountDto Foreign { get; set; } = new();
}

public class ShipmentStatusReportDto
{
    public int NewCount { get; set; }
    public int AtPortCount { get; set; }
    public int ClearedCount { get; set; }
    public int InTransitCount { get; set; }
    public int ArrivedCount { get; set; }
}