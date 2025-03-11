using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.Routes;

public class Route : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; }
    public Guid OperationId { get; set; }
    public Operation Operation { get; set; }
    [StringLength(50)] public string EstimatedTime { get; set; }
    public Guid? WorkflowId { get; set; }
    public Form WorkFlow { get; set; }
    public int Order { get; set; }
    public List<RouteResource> Resources { get; set; }
    public List<RouteResponsibleUser> ResponsibleUsers { get; set; } = [];
    public List<RouteResponsibleRole> ResponsibleRoles { get; set; } = [];
    public List<RouteWorkCenter> WorkCenters { get; set; } = [];
}

public class RouteResource : BaseEntity
{
    public Guid RouteId { get; set; }
    public Route Route { get; set; }
    public Guid ResourceId { get; set; }
    public Resource Resource { get; set; }
}

public class RouteResponsibleUser : BaseEntity
{
    public Guid RouteId { get; set; }
    public Route Route { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; }
}

public class RouteResponsibleRole : BaseEntity
{
    public Guid RouteId { get; set; }
    public Route Route { get; set; }
    public Guid RoleId { get; set; }
    public Role Role { get; set; }
}

public class RouteWorkCenter : BaseEntity
{
    public Guid RouteId { get; set; }
    public Route Route { get; set; }
    public Guid WorkCenterId { get; set; }
    public WorkCenter WorkCenter { get; set; }
}