namespace DOMAIN.Entities.Routes;

public class CreateRouteRequest
{
    public Guid OperationId { get; set; }
    public string EstimatedTime { get; set; }
    public List<CreateRouteResource> Resources { get; set; } = [];
    public List<CreateRouteResponsibleUser> ResponsibleUsers { get; set; } = [];
    public List<CreateRouteResponsibleRole> ResponsibleRoles { get; set; } = [];
    public List<CreateRouteWorkCenter> WorkCenters { get; set; } = [];
    public int Order { get; set; }
}

public class CreateRouteResource
{
    public Guid ResourceId { get; set; }
}

public class CreateRouteResponsibleUser
{
    public Guid UserId { get; set; }
    public List<CreateRouteOperationAction> Actions { get; set; } = [];
}


public class CreateRouteResponsibleRole 
{
    public Guid RoleId { get; set; }
    public List<CreateRouteOperationAction> Actions { get; set; } = [];
}

public class CreateRouteWorkCenter
{
    public Guid WorkCenterId { get; set; }
}

public class CreateRouteOperationAction
{
    public Guid? ProductAnalyticalRawDataId { get; set; }
    public OperationAction Action { get; set; }
}