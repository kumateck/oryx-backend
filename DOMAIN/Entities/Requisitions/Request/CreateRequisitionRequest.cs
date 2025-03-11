namespace DOMAIN.Entities.Requisitions.Request;

public class CreateRequisitionRequest
{
    public string Code { get; set; }
    public RequisitionType RequisitionType { get; set; }
    public Guid? ProductId { get; set; }
    public Guid? ProductionScheduleId { get; set; }
    public Guid? ProductionActivityStepId { get; set; }
    public string Comments { get; set; }
    public DateTime? ExpectedDelivery { get; set; }
    public List<CreateRequisitionItemRequest> Items { get; set; } = [];
}

public class CreateRequisitionItemRequest
{
    public Guid MaterialId { get; set; }
    public decimal Quantity { get; set; }
    public Guid? UomId { get; set; }
}