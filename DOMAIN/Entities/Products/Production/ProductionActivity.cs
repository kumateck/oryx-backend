using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Routes;
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
    public ProductionStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class ProductionActivityStep : BaseEntity
{
    public Guid ProductionActivityId { get; set; }
    public ProductionActivity ProductionActivity { get; set; }
    public Guid ProductRouteId { get; set; }
    public Route ProductRoute { get; set; }
    public List<ProuductionActivityStepUser> ResponsibleUsers { get; set; } = [];
    public ProductionStatus Status { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class ProuductionActivityStepUser : BaseEntity
{
    public Guid ProductionActivityStepId { get; set; }
    public ProductionActivityStep ProductionActivityStep { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; }
}

public class ProductionActivityDto : BaseDto
{
    public CollectionItemDto ProductionSchedule { get; set; }
    public CollectionItemDto Product { get; set; }
    public List<ProductionActivityStepDto> Steps { get; set; } = [];
    public ProductionStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class ProductionActivityStepDto : BaseDto
{
    public CollectionItemDto ProductionActivity { get; set; }
    public CollectionItemDto ProductRoute { get; set; }
    public List<ProuductionActivityStepUserDto> ResponsibleUsers { get; set; } = [];
    public ProductionStatus Status { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class ProuductionActivityStepUserDto : BaseDto
{
    public UserDto User { get; set; }
}