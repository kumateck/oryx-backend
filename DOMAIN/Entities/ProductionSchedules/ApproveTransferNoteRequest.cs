namespace DOMAIN.Entities.ProductionSchedules;

public class ApproveTransferNoteRequest
{
    public int QuantityReceived { get; set; }
    
    public string Notes { get; set; }
}