using DOMAIN.Entities.Base;
using SHARED;

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
    public int NumberOfIncomingStockTransfers { get; set; }
    public int NumberOfIncomingPendingStockTransfers { get; set; }
    public int NumberOfIncomingCompletedStockTransfers { get; set; }
    public int NumberOfOutgoingStockTransfers { get; set; }
    public int NumberOfOutgoingPendingStockTransfers { get; set; }
    public int NumberOfOutgoingCompletedStockTransfers { get; set; }
}

public class WarehouseReportDto
{
    public int NumberOfStockRequisitions { get; set; }
    public int NumberOfNewStockRequisitions { get; set; }
    public int NumberOfInProgressStockRequisitions { get; set; }
    public int NumberOfCompletedStockRequisitions { get; set; }
    public int NumberOfIncomingStockTransfers { get; set; }
    public int NumberOfIncomingPendingStockTransfers { get; set; }
    public int NumberOfIncomingCompletedStockTransfers { get; set; }
    public int NumberOfShipments { get; set; }
    public int NumberOfInTransitShipments { get; set; }
    public int NumberOfArrivedShipments { get; set; }
    public int NumberOfClearedShipments { get; set; }
}

public class MaterialBatchReservedQuantityReportDto
{
    public CollectionItemDto Warehouse { get; set; }
    public CollectionItemDto Material { get; set; }
    public UnitOfMeasureDto UoM { get; set; }
    public decimal Quantity { get; set; }
}

public class LogisticsReportDto
{
    public int NumberOfInvoices { get; set; }
    public int NumberOfPaidInvoices { get; set; }
    public int NumberOfUnpaidInvoices { get; set; }
    public int NumberOfShipments { get; set; }
    public int NumberOfNewShipments { get; set; }
    public int NumberOfInTransitShipments { get; set; }
    public int NumberOfArrivedShipments { get; set; }
    public int NumberOfClearedShipments { get; set; }
    public int NumberOfBillingSheets { get; set; }
    public int NumberOfPaidBillingSheets { get; set; }
    public int NumberOfPendingBillingSheets { get; set; }

    public int NumberOfWaybills { get; set; }
    public int NumberOfNewWaybills { get; set; }
    public int NumberOfInTransitWaybills { get; set; }
    public int NumberOfArrivedWaybills { get; set; }
    public int NumberOfClearedWaybills { get; set; }
}



public class ProductionDashboardDto
{
    public RequisitionReportDto RequisitionReport { get; set; }
    public List<MaterialReorderReportDto> MaterialsBelowReorderLevel { get; set; }
    public ProductionScheduleStatusReportDto ProductionScheduleReport { get; set; }
    public StockTransferStatusReportDto StockTransferReport { get; set; }
}

public class RequisitionReportDto
{
    public int NewRequisitionsCount { get; set; }
    public int PendingRequisitionsCount { get; set; }
    public int RejectedRequisitionsCount { get; set; }
    public int CompletedRequisitionsCount { get; set; }
    public int SourcedRequisitionsCount { get; set; }
   
}
public class MaterialReorderReportDto
{
    public string MaterialName { get; set; } 
    public string MaterialCode { get; set; } 
    public decimal CurrentQuantity { get; set; }
    public decimal ReOrderLevel { get; set; }
    public string UomSymbol { get; set; }
}

public class StockTransferStatusReportDto
{
    public int InProgressCount { get; set; }
    public int ApprovedCount { get; set; }
    public int IssuedCount { get; set; }
    public int RejectedCount { get; set; }
}

public class ProductionScheduleStatusReportDto
{
    public int NewScheduleCount { get; set; }
    public int InProgressScheduleCount { get; set; }
    public int CompletedScheduleCount { get; set; }
    public int DelayedScheduleCount { get; set; }
    public int CancelledScheduleCount { get; set; }
}