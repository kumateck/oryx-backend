using DOMAIN.Entities.Base;
using DOMAIN.Entities.ProductAnalyticalRawData;
using SHARED;

namespace DOMAIN.Entities.Routes;

public class RouteDto : BaseDto
{
    public CollectionItemDto Operation { get; set; }
    public string EstimatedTime { get; set; }
    public List<RouteResourceDto> Resources { get; } = [];
    public int Order { get; set; }
    public List<RouteResponsibleUserDto> ResponsibleUsers { get; set; } = [];
    public List<RouteResponsibleRoleDto> ResponsibleRoles { get; set; } = [];
    public List<RouteWorkCenterDto> WorkCenters { get; set; } = [];
}

public class RouteResourceDto 
{
    public CollectionItemDto Resource { get; set; }
}

public class RouteResponsibleUserDto 
{
    public CollectionItemDto User { get; set; }
    public List<RouteOperationActionDto> Actions { get; set; } = [];
}


public class RouteResponsibleRoleDto
{
    public CollectionItemDto Role { get; set; }
    public List<RouteOperationActionDto> Actions { get; set; } = [];
}

public class RouteWorkCenterDto
{
    public CollectionItemDto WorkCenter { get; set; }
}

public class RouteOperationActionDto
{
    public ProductAnalyticalRawDataDto ProductAnalyticalRawData{ get; set; }
    public OperationAction Action { get; set; }
}