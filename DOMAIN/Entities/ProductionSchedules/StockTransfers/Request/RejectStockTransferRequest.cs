namespace DOMAIN.Entities.ProductionSchedules.StockTransfers.Request;

/// <summary>Why the supplying store will not issue a transfer. Required, 1–1000 characters.</summary>
public class RejectStockTransferRequest
{
    public string Reason { get; set; }
}
