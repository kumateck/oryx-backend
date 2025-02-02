using SHARED;

namespace DOMAIN.Entities.Routes;

public class RouteDto
{
    public CollectionItemDto Operation { get; set; }
    public CollectionItemDto WorkCenter { get; set; }
    public string EstimatedTime { get; set; }
    public List<CollectionItemDto> Resources { get; } = [];
    public int Order { get; set; }
    public List<RouteResponsiblePartyDto> ResponsibleParties { get; set; } = [];
}

public class RouteResponsiblePartyDto 
{
    public CollectionItemDto User { get; set; }
    public CollectionItemDto Role { get; set; }
}