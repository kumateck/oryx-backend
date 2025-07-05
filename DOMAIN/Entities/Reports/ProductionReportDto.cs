namespace DOMAIN.Entities.Reports;

public class ProductionReportDto
{
    public int NumberOfPurchaseRequisitions { get; set; }
    public int NumberOfNewPurchaseRequisitions { get; set; }
    public int NumberOfInProgressPurchaseRequisitions { get; set; }
    public int NumberOfCompletedPurchaseRequisitions { get; set; }
    public int NumberOfProductionSchedules { get; set; }
    public int NumberOfNewProductionSchedules { get; set; }
    public int NumberOfInProgressProductionSchedules { get; set; }
    public int NumberOfCompletedProductionSchedules { get; set; }
    public int NumberOfIncomingStockTransfers {get; set;}
    public int NumberOfIncomingPendingStockTransfers {get; set;}
    public int NumberOfIncomingCompletedStockTransfers {get; set;}
    public int NumberOfOutgoingStockTransfers {get; set;}
    public int NumberOfOutgoingPendingStockTransfers {get; set;}
    public int NumberOfOutgoingCompletedStockTransfers {get; set;}
}