namespace DOMAIN.Entities.ProductionSchedules;

public class ApproveTransferNoteRequest
{
    public decimal QuantityReceived { get; set; }
    public string Notes { get; set; }
    public decimal Loose { get; set; }
}