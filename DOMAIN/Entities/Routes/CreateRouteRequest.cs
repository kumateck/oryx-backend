namespace DOMAIN.Entities.Routes;

public class CreateRouteRequest
{
    public Guid OperationId { get; set; }
    public Guid WorkCenterId { get; set; }
    public string EstimatedTime { get; set; }
    public List<Guid> ResourceIds { get; set; } = [];
    public List<CreateRouteResponsibleParty> ResponsibleParties { get; set; } = [];
    public int Order { get; set; }
}

public class CreateRouteResponsibleParty 
{
    public Guid? UserId { get; set; }
    public Guid? RoleId { get; set; }
}