namespace SHARED;

public class BatchInconsistencyReport
{
    public Guid BatchId { get; set; }
    public string BatchNumber { get; set; }
    public string Status { get; set; }
    public Guid? WarehouseId { get; set; }
    public string WarehouseName { get; set; }
    
    // The aggregates from the MaterialBatches table
    public decimal GlobalTotalQuantity { get; set; }
    public decimal GlobalConsumedQuantity { get; set; }
    public decimal GlobalQuantityAssigned { get; set; }
    
    // The actual values from the ledger
    public decimal ActualShelfQuantity { get; set; }
    public decimal ActualConsumedQuantity { get; set; }
    
    // Calculated "Virtual" state
    public decimal UnassignedQuantity { get; set; } // Total - Consumed - Assigned
    
    // Flags
    public bool IsOverAssigned => UnassignedQuantity < 0;
    public bool HasStatusMismatch { get; set; } // e.g., Available but 0 quantity
}
