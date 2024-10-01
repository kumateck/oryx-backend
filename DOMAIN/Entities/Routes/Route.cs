using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Products;

namespace DOMAIN.Entities.Routes;

public class Route : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; }
    public Guid OperationId { get; set; }
    public Operation Operation { get; set; }
    public Guid WorkCenterId { get; set; }
    public WorkCenter WorkCenter { get; set; }
    [StringLength(50)] public string EstimatedTime { get; set; }
    public int Order { get; set; }
    public List<RouteResource> Resources { get; set; }
}

public class RouteResource : BaseEntity
{
    public Guid RouteId { get; set; }
    public Route Route { get; set; }
    public Guid ResourceId { get; set; }
    public Resource Resource { get; set; }
}