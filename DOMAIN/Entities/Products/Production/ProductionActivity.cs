using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Users;
using SHARED;

namespace DOMAIN.Entities.Products.Production;

public class ProductionActivity : BaseEntity
{
    [StringLength(100)] public string Code { get; set; }
    public Guid ProductionScheduleId { get; set; }
    public ProductionSchedule ProductionSchedule { get; set; }
    public Guid ProductId { get; set; }
    public Product Product { get; set; }
    public List<ProductionActivityStep> Steps { get; set; } = [];
    public ProductionStatus Status { get; set; } = ProductionStatus.New;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<ProductionActivityLog> ActivityLogs { get; set; } = [];
}

public class ProductionActivityStep : BaseEntity
{
    public Guid ProductionActivityId { get; set; }
    public ProductionActivity ProductionActivity { get; set; }
    public Guid OperationId { get; set; }
    public Operation Operation { get; set; }
    public Guid? WorkflowId { get; set; }
    public Form WorkFlow { get; set; }
    public int Order { get; set; }
    public List<ProductionActivityStepResource> Resources { get; set; } = [];
    public List<ProductionActivityStepWorkCenter> WorkCenters { get; set; } = [];
    public List<ProductionActivityStepUser> ResponsibleUsers { get; set; } = [];
    public ProductionStatus Status { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class ProductionActivityStepUser : BaseEntity
{
    public Guid ProductionActivityStepId { get; set; }
    public ProductionActivityStep ProductionActivityStep { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; }
}

public class ProductionActivityStepResource : BaseEntity
{
    public Guid ProductionActivityStepId { get; set; }
    public ProductionActivityStep ProductionActivityStep { get; set; }
    public Guid ResourceId { get; set; }
    public Resource Resource { get; set; }
}

public class ProductionActivityStepWorkCenter : BaseEntity
{
    public Guid ProductionActivityStepId { get; set; }
    public ProductionActivityStep ProductionActivityStep { get; set; }
    public Guid WorkCenterId { get; set; }
    public WorkCenter WorkCenter { get; set; }
}

public class ProductionActivityLog : BaseEntity
{
    public Guid ProductionActivityId { get; set; }
    public ProductionActivity ProductionActivity { get; set; }
    [StringLength(1000)] public string Message { get; set; }  
    public Guid? UserId { get; set; }  
    public User User { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}


public class ProductionActivityDto : ProductionActivityListDto
{
    public List<ProductionActivityLogDto> ActivityLogs { get; set; } = [];
}

public class ProductionActivityListDto : BaseDto
{
    public CollectionItemDto ProductionSchedule { get; set; }
    public CollectionItemDto Product { get; set; }
    public List<ProductionActivityStepDto> Steps { get; set; } = [];
    public ProductionStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public ProductionActivityStepDto CurrentStep =>
        Steps.Count != 0
            ? Steps.OrderBy(s => s.Order).FirstOrDefault(s => !s.CompletedAt.HasValue) ?? Steps.OrderBy(s => s.Order).Last()
            : null;
}

public class ProductionActivityGroupDto : BaseDto
{
    public CollectionItemDto ProductionSchedule { get; set; }
    public ProductListDto Product { get; set; }
    public ProductionStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public ProductionActivityStepDto CurrentStep { get; set; }
    public string BatchNumber { get; set; }
    public decimal Quantity { get; set; }
}

public class ProductionActivityGroupResultDto
{
    public OperationDto Operation { get; set; } // Operation name and ID
    public List<ProductionActivityGroupDto> Activities { get; set; } = [];
}


public class ProductionActivityStepDto : BaseDto
{
    public CollectionItemDto ProductionActivity { get; set; }
    public OperationDto Operation { get; set; }
    public CollectionItemDto WorkFlow { get; set; }
    public List<ProductionActivityStepResourceDto> Resources { get; set; } = [];
    public List<ProductionActivityStepWorkCenterDto> WorkCenters { get; set; } = [];
    public List<ProductionActivityStepUserDto> ResponsibleUsers { get; set; } = [];
    public ProductionStatus Status { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int Order { get; set; }
}

public class ProductionActivityStepUserDto : BaseDto
{
    public UserDto User { get; set; }
}

public class ProductionActivityStepResourceDto : BaseDto
{
    public ResourceDto Resource { get; set; }
}

public class ProductionActivityStepWorkCenterDto : BaseDto
{
    public CollectionItemDto WorkCenter { get; set; }
}

public class ProductionActivityLogDto : BaseDto
{
    public string Message { get; set; }  
    public UserDto User { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}