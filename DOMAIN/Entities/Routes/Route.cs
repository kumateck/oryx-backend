using DOMAIN.Entities.Base;
using DOMAIN.Entities.BillOfMaterials;

namespace DOMAIN.Entities.Routes;

public class Route : BaseEntity
{
    public Guid OperationId { get; set; }
    public Operation Operation { get; set; }
    public Guid WorkCenterId { get; set; }
    public WorkCenter WorkCenter { get; set; }
    public Guid BillOfMaterialItemId { get; set; }
    public BillOfMaterialItem BillOfMaterialItem { get; set; }
    public TimeSpan EstimatedTime { get; set; }
    public List<RouteResource> Resources { get; set; }
}

public class RouteResource : BaseEntity
{
    public Guid RouteId { get; set; }
    public Route Route { get; set; }
    public Guid ResourceId { get; set; }
    public Resource Resource { get; set; }
}