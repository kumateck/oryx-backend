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
    public int NumberOfIncomingStockTransfers {get; set;}
    public int NumberOfIncomingPendingStockTransfers {get; set;}
    public int NumberOfIncomingCompletedStockTransfers {get; set;}
    public int NumberOfOutgoingStockTransfers {get; set;}
    public int NumberOfOutgoingPendingStockTransfers {get; set;}
    public int NumberOfOutgoingCompletedStockTransfers {get; set;}
}

public class WarehouseReportDto
{
    public int NumberOfStockRequisitions { get; set; }
    public int NumberOfNewStockRequisitions { get; set; }
    public int NumberOfInProgressStockRequisitions { get; set; }
    public int NumberOfCompletedStockRequisitions { get; set; }
    public int NumberOfIncomingStockTransfers {get; set;}
    public int NumberOfIncomingPendingStockTransfers {get; set;}
    public int NumberOfIncomingCompletedStockTransfers {get; set;}
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
    
}
