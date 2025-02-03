using SHARED;

namespace DOMAIN.Entities.Routes;

public class RouteDto
{
    public CollectionItemDto Operation { get; set; }
    public CollectionItemDto WorkCenter { get; set; }
    public string EstimatedTime { get; set; }
    public List<CollectionItemDto> Resources { get; } = [];
    public int Order { get; set; }
    public List<RouteResponsibleUserDto> ResponsibleUsers { get; set; } = [];
    public List<RouteResponsibleRole> ResponsibleRoles { get; set; } = [];
    public List<RouteWorkCenterDto> WorkCenters { get; set; } = [];
}

public class RouteResponsibleUserDto 
{
    public CollectionItemDto User { get; set; }
}


public class RouteResponsibleRoleDto
{
    public CollectionItemDto Role { get; set; }
}

public class RouteWorkCenterDto
{
    public CollectionItemDto WorkCenter { get; set; }
}