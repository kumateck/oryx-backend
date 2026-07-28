namespace DOMAIN.Entities.Reports.Warehouse;

public class MaterialsStockSummaryDto
{
    public int No { get; set; }
    public string MaterialName { get; set; }
    public string MaterialCode { get; set; }
    public string MaterialType { get; set; }
    public string ProductionDepartment { get; set; }
    public string WarehouseType { get; set; }
    public int NoOfBatches { get; set; }
    public decimal TotalQuantity { get; set; }
    public string UOM { get; set; }
}

public class MaterialsStockBatchDetailDto
{
    public int No { get; set; }
    public string MaterialName { get; set; }
    public string MaterialCode { get; set; }
    public string MaterialType { get; set; }
    public string BatchNo { get; set; }
    public DateTime? ManufacturingDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal QuantityAvailable { get; set; }
    public string UOM { get; set; }
    public string ProductionDepartment { get; set; }
    public string WarehouseType { get; set; }
    public string StorageLocation { get; set; }
}

public enum OccupancyStatus
{
    Empty,
    Single,
    Multiple
}

public class ShelfUtilisationDetailDto
{
    public int No { get; set; }
    public string Warehouse { get; set; }
    public string WarehouseType { get; set; }
    public string Location { get; set; }
    public string Rack { get; set; }
    public string ShelfCode { get; set; }
    public string ShelfName { get; set; }
    public string OccupancyStatus { get; set; }
    public int DistinctBatches { get; set; }
    public decimal TotalQuantity { get; set; }
    public DateTime? LastUpdated { get; set; }
}

public class GoodsReceivingRegisterDto
{
    public int No { get; set; }
    public string GrnNumber { get; set; }
    public string GrnStatus { get; set; }
    public DateTime GrnGeneratedAt { get; set; }
    public string CarrierName { get; set; }
    public string VehicleNumber { get; set; }
    public string DeclarationNumber { get; set; }
    public string Remarks { get; set; }
    public string Supplier { get; set; }
    public string Material { get; set; }
    public decimal QuantityReceived { get; set; }
    public string UOM { get; set; }
    public string MaterialBatches { get; set; }
}

public class ReceivingPerformanceDetailDto
{
    public int No { get; set; }
    public string Warehouse { get; set; }
    public string Supplier { get; set; }
    public string Material { get; set; }
    public decimal Quantity { get; set; }
    public DateTime? ArrivedAt { get; set; }
    public DateTime? CheckedAt { get; set; }
    public DateTime? GrnGeneratedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? DistributedAt { get; set; }
    public DateTime? AssignedAt { get; set; }
    public double? ArrivalToCheckHours { get; set; }
    public double? CheckToGrnHours { get; set; }
    public double? GrnToApprovedHours { get; set; }
    public double? GrnToDistributeHours { get; set; }
    public double? DistributeToAssignedHours { get; set; }
    public double? TotalDockToStockHours { get; set; }
}

public class PutawayRegisterDto
{
    public int No { get; set; }
    public string Material { get; set; }
    public string BatchNo { get; set; }
    public string Quantity { get; set; }
    public string UOM { get; set; }
    public string FromLocation { get; set; }
    public string ToShelf { get; set; }
    public DateTime PutawayDate { get; set; }
    public string PutawayBy { get; set; }
    public string Note { get; set; }
}

public class StockAdjustmentAuditDto
{
    public int No { get; set; }
    public string AdjustmentNo { get; set; }
    public DateTime AdjustmentDate { get; set; }
    public string Material { get; set; }
    public string BatchNo { get; set; }
    public string Shelf { get; set; }
    public decimal SystemQuantity { get; set; }
    public decimal PhysicalCount { get; set; }
    public decimal Variance { get; set; }
    public string ReasonCode { get; set; }
    public string Notes { get; set; }
    public string AdjustedBy { get; set; }
}

public class BatchTraceabilityDto
{
    public int No { get; set; }
    public string EventType { get; set; }
    public DateTime EventDate { get; set; }
    public string FromLocation { get; set; }
    public string ToLocation { get; set; }
    public decimal Quantity { get; set; }
    public string UOM { get; set; }
    public string ReferenceDocument { get; set; }
    public string PerformedBy { get; set; }
    public string Remarks { get; set; }
}

public class InterWarehouseSwapRequestDto
{
    public int No { get; set; }
    public string FirstWarehouse { get; set; }
    public string SecondWarehouse { get; set; }
    public string Status { get; set; }
    public string Material { get; set; }
    public string BatchNo { get; set; }
    public decimal Quantity { get; set; }
    public string UOM { get; set; }
    public string Requester { get; set; }
    public string ActionedBy { get; set; }
    public DateTime? ActionedAt { get; set; }
    public string ActionNote { get; set; }
}

public class StockTransferInterDepartmentDetailDto
{
    public int No { get; set; }
    public string TransferCode { get; set; }
    public string Material { get; set; }
    public string UOM { get; set; }
    public decimal RequiredQuantity { get; set; }
    public string FromDepartment { get; set; }
    public string ToDepartment { get; set; }
    public string Status { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string ApprovedBy { get; set; }
    public DateTime? IssuedAt { get; set; }
    public string IssuedBy { get; set; }
    public string Reason { get; set; }
}

public class MaterialExpiryProjectionDto
{
    public int No { get; set; }
    public string Material { get; set; }
    public string MaterialCode { get; set; }
    public string BatchNo { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public int? DaysUntilExpiry { get; set; }
    public string ExpiryWindow { get; set; }
    public decimal Quantity { get; set; }
    public string UOM { get; set; }
    public string Warehouse { get; set; }
    public string Shelf { get; set; }
    public int DaysOnShelf { get; set; }
}

public enum InactivityThreshold
{
    Days90 = 90,
    Days180 = 180,
    Days365 = 365
}

public class SlowMovingInventoryDto
{
    public int No { get; set; }
    public string Material { get; set; }
    public string MaterialCode { get; set; }
    public string BatchNo { get; set; }
    public decimal Quantity { get; set; }
    public string UOM { get; set; }
    public DateTime? LastMovementDate { get; set; }
    public int DaysSinceLastMove { get; set; }
    public string Warehouse { get; set; }
    public string Shelf { get; set; }
    public decimal EstValue { get; set; }
}

public class BinCardTransactionLedgerDto
{
    public int No { get; set; }
    public DateTime Date { get; set; }
    public string Description { get; set; }
    public string WayBill { get; set; }
    public string ArNumber { get; set; }
    public string Supplier { get; set; }
    public string Manufacturer { get; set; }
    public decimal QuantityReceived { get; set; }
    public decimal QuantityIssued { get; set; }
    public decimal BalanceQuantity { get; set; }
    public string UOM { get; set; }
    public string Product { get; set; }
}

public class OperationsSummaryDto
{
    public string Department { get; set; }
    public string Period { get; set; }
    public int MaterialsReceived { get; set; }
    public int GrnsCreated { get; set; }
    public int BatchesPutAway { get; set; }
    public int StockAdjustments { get; set; }
    public int StockTransfers { get; set; }
    public int SwapsApproved { get; set; }
    public int ChecklistsDone { get; set; }
}

public class QcPendingDto
{
    public int No { get; set; }
    public string Material { get; set; }
    public string MaterialCode { get; set; }
    public string Supplier { get; set; }
    public decimal Quantity { get; set; }
    public string UOM { get; set; }
    public DateTime? ChecklistDate { get; set; }
    public int DaysWaiting { get; set; }
    public string Warehouse { get; set; }
    public string ArrivalLocation { get; set; }
}

public class ReorderLevelVsStockDto
{
    public int No { get; set; }
    public string Material { get; set; }
    public string MaterialCode { get; set; }
    public string MaterialType { get; set; }
    public decimal CurrentStock { get; set; }
    public decimal ReorderLevel { get; set; }
    public decimal AvgMonthlyConsumption { get; set; }
    public decimal MonthsOfStock { get; set; }
    public decimal SuggestedOrderQty { get; set; }
    public string StockOutRisk { get; set; }
    public string UOM { get; set; }
    public string Department { get; set; }
}

public class InventoryValuationSummaryDto
{
    public int No { get; set; }
    public string WarehouseType { get; set; }
    public string Division { get; set; }
    public string Warehouse { get; set; }
    public int ItemMaterialCount { get; set; }
    public string UomGroup { get; set; }
    public string TotalQuantity { get; set; }
    public decimal EstUnitCost { get; set; }
    public string TotalValue { get; set; }
    public decimal PercentOfTotalInventory { get; set; }
}

public class InventoryValuationDetailDto
{
    public int No { get; set; }
    public string WarehouseType { get; set; }
    public string Division { get; set; }
    public string Warehouse { get; set; }
    public string MaterialItem { get; set; }
    public string Code { get; set; }
    public string BatchNo { get; set; }
    public string ValuationType { get; set; }
    public string UOM { get; set; }
    public string QuantityUOM { get; set; }
    public decimal? UnitSize { get; set; }
    public decimal ItemCount { get; set; }
    public decimal EstUnitCostVal { get; set; }
    public string CostBasis { get; set; }
    public string TotalValue { get; set; }
    public string ShelfLocation { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public int DaysOnShelf { get; set; }
}

public class WarehouseEmployeeActivityDto
{
    public int No { get; set; }
    public string Employee { get; set; }
    public string Department { get; set; }
    public int ChecklistsDone { get; set; }
    public int GrnsCreated { get; set; }
    public int StockAdjustments { get; set; }
    public int MaterialDistributions { get; set; }
    public int SwapsActioned { get; set; }
    public int PutawayActions { get; set; }
    public int TotalActions { get; set; }
}

public class ArrivalLocationStatusDto
{
    public int No { get; set; }
    public string Warehouse { get; set; }
    public string ArrivalLocation { get; set; }
    public string Material { get; set; }
    public string BatchNo { get; set; }
    public decimal Quantity { get; set; }
    public string UOM { get; set; }
    public int DaysInArrival { get; set; }
    public string Status { get; set; }
    public string PriorityFlag { get; set; }
}
