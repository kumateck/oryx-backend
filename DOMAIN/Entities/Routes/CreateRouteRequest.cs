namespace DOMAIN.Entities.Routes;

public class CreateRouteRequest
{
    public Guid OperationId { get; set; }
    public Guid WorkCenterId { get; set; }
    public Guid BillOfMaterialItemId { get; set; }
    public string EstimatedTime { get; set; }
    public List<Guid> ResourceIds { get; set; }
    public int Order { get; set; }
}