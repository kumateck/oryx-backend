using DOMAIN.Entities.Base;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Routes;

namespace DOMAIN.Entities.Products.Production;

public class ProductionActivity : BaseEntity
{
    public Guid ProductionScheduleId { get; set; }
    public ProductionSchedule ProductionSchedule { get; set; }
    public Guid ProductId { get; set; }
    public Product Product { get; set; }
    public List<ProductionActivityStep> Steps { get; set; } = [];
    public ProductionStatus Status { get; set; }
}

public enum ProductionStatus
{
    New = 0,
    InProgress = 1,
    Completed = 2
}

public class ProductionActivityStep : BaseEntity
{
    public Guid ProductionActivityId { get; set; }
    public ProductionActivity ProductionActivity { get; set; }
    public Guid ProductRouteId { get; set; }
    public Route ProductRoute { get; set; }
    public List<ProuductionActivityStepUser> ResponsibleUsers { get; set; } = [];
    
    
}

public class ProuductionActivityStepUser : BaseEntity
{
    public Guid ProductionActivityStepId { get; set; }
    public ProductionActivityStep ProductionActivityStep { get; set; }
}