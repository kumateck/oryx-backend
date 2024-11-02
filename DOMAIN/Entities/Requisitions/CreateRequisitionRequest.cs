namespace DOMAIN.Entities.Requisitions;

public class CreateRequisitionRequest
{
    public RequisitionType RequisitionType { get; set; }
    public string Comments { get; set; }
    public List<CreateRequisitionItemRequest> Items { get; set; } = [];
}

public class CreateRequisitionItemRequest
{
    public Guid MaterialId { get; set; }
    public int Quantity { get; set; }
    public Guid? UomId { get; set; }
}