namespace DOMAIN.Entities.Requisitions;

public class ApproveRequisitionRequest
{
    public Guid RequisitionId { get; set; }
    public string Comments { get; set; }
}