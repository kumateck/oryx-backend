namespace DOMAIN.Entities.Requisitions;

public class CreateRequisitionRequest
{
    public Guid MaterialId { get; set; }
    public int Quantity { get; set; }
    public RequisitionType RequisitionType { get; set; }
    public string Comments { get; set; }
}