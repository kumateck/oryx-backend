namespace DOMAIN.Entities.ProductionSchedules.StockTransfers.Request;

public class CreateStockTransferRequest
{
    public Guid MaterialId { get; set; }
    public string Reason { get; set; }
    public decimal RequiredQuantity { get; set; }
    public Guid? ProductId { get; set; }
    public Guid? ProductionScheduleId { get; set; }
    public Guid? ProductionActivityStepId { get; set; }
    public List<StockTransferSourceRequest> Sources { get; set; } = [];
}

public class StockTransferSourceRequest
{
    public Guid FromDepartmentId { get; set; }
    public decimal Quantity { get; set; }
    public Guid UoMId { get; set; }
}

public class IssueStockTransferRequest
{
    public Guid StockTransferId { get; set; }
    public List<BatchTransferRequest> Batches { get; set; } = [];
}

public class BatchTransferRequest
{
    public Guid BatchId { get; set; }
    public Guid FromWarehouseId { get; set; }
    public Guid ToWarehouseId { get; set; }
    public decimal Quantity { get; set; }
}