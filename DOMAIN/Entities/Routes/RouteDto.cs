using SHARED;

namespace DOMAIN.Entities.Routes;

public class RouteDto
{
    public CollectionItemDto Operation { get; set; }
    public CollectionItemDto WorkCenter { get; set; }
    public string EstimatedTime { get; set; }
    public List<CollectionItemDto> Resources { get; } = [];
    public int Order { get; set; }
}