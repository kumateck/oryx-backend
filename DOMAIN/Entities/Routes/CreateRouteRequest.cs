namespace DOMAIN.Entities.Routes;

public class CreateRouteRequest
{
    public Guid OperationId { get; set; }
    public Guid WorkCenterId { get; set; }
    public Guid BillOfMaterialItemId { get; set; }
    public TimeSpan EstimatedTime { get; set; }
    public List<Guid> ResourceIds { get; set; }
}