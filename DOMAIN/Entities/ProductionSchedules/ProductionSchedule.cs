using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Customers;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.ProductionSchedules;

public class ProductionSchedule : BaseEntity
{
    [StringLength(100)] public string Code { get; set; }
    public DateTime ScheduledStartTime { get; set; }
    public DateTime ScheduledEndTime { get; set; }
    public ProductionStatus Status { get; set; }
    [StringLength(1000)] public string Remarks { get; set; }
    public Guid? DepartmentId { get; set; }
    public Department Department { get; set; }
    public List<ProductionScheduleProduct> Products { get; set; } = [];
}

public class ProductionScheduleItem : BaseEntity
{
    public Guid ProductionScheduleId { get; set; }
    public ProductionSchedule ProductionSchedule { get; set; }
    public Guid? UomId { get; set; }
    public UnitOfMeasure UoM { get; set; }
    public Guid MaterialId { get; set; }
    public Material Material { get; set; }
    public decimal Quantity { get; set; }
}

public enum BatchSize
{
    Full,
    Half
}

public class ProductionScheduleProduct
{
    public Guid Id { get; set; }
    public Guid ProductionScheduleId { get; set; }
    public ProductionSchedule ProductionSchedule { get; set; }
    public Guid ProductId { get; set; }
    public Product Product { get; set; }
    [StringLength(100)] public string BatchNumber { get; set; }
    public BatchSize BatchSize { get; set; }
    public Guid? MarketTypeId { get; set; }
    public Customer MarketType { get; set; }
    public decimal Quantity { get; set; }
    public bool Cancelled { get; set; }
    [StringLength(20000)] public string ReasonForCancellation { get; set; }
    public Guid? ProductPackingId { get; set; }
    public ProductPacking ProductPacking { get; set; }
}

public class MarketType : BaseEntity
{
    [StringLength(1000)] public string Name { get; set; }
}

public class CreateProductionExtraPacking
{
    public Guid MaterialId { get; set; }
    public Guid UoMId { get; set; }
    public decimal Quantity { get; set; }
}

public class ProductionExtraPacking : BaseEntity, IRequireApproval
{
    public Guid ProductionScheduleProductId { get; set; }
    public ProductionScheduleProduct ProductionScheduleProduct { get; set; }
    public Guid MaterialId { get; set; }
    public Material Material { get; set; }
    public Guid UoMId { get; set; }
    public UnitOfMeasure UoM { get; set; }
    public decimal Quantity { get; set; }
    public ProductionExtraPackingStatus Status { get; set; }
    public DateTime? IssuedAt { get; set; }
    public Guid? IssuedById { get; set; }
    public User IssuedBy { get; set; }
    public bool Approved { get; set; }
    public List<ProductionExtraPackingApproval> Approvals { get; set; } = [];
}

public class ProductionExtraPackingApproval : ResponsibleApprovalStage
{
    public Guid Id { get; set; }
    public Guid ProductionExtraPackingId { get; set; }
    public ProductionExtraPacking ProductionExtraPacking { get; set; }
    public Guid ApprovalId { get; set; }
    public Approval Approval { get; set; }
}

public enum ProductionExtraPackingStatus
{
    Pending = 0,
    InProgress = 1,
    Approved = 2
}


public class ProductionExtraPackingDto : BaseDto
{
    public ProductionScheduleProductDto ProductionScheduleProduct { get; set; }
    public MaterialDto Material { get; set; }
    public UnitOfMeasureDto UoM { get; set; }
    public ProductionExtraPackingStatus Status { get; set; }
    public decimal Quantity { get; set; }
    public bool Approved { get; set; }
}

public class ProductionExtraPackingWithBatchesDto : ProductionExtraPackingDto
{
    public List<BatchToSupply> Batches { get; set; } = [];
}