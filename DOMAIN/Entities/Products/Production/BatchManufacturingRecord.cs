using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Users;
using SHARED;

namespace DOMAIN.Entities.Products.Production;

public class CreateBatchManufacturingRecord
{
    public Guid ProductId { get; set; }
    public Guid ProductionScheduleId { get; set; }
    public Guid ProductionActivityStepId { get; set; }
    public string BatchNumber { get; set; }
    public DateTime? ManufacturingDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal BatchQuantity { get; set; }
}

public class UpdateBatchManufacturingRecord
{ 
    public string BatchNumber { get; set; }
    public DateTime? ManufacturingDate { get; set; } 
    public DateTime? ExpiryDate { get; set; } 
    public decimal BatchQuantity { get; set; }
}

public class BatchManufacturingRecord : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; }
    public Guid ProductionScheduleId { get; set; }
    public ProductionSchedule ProductionSchedule { get; set; }
    public Guid ProductionActivityStepId { get; set; }
    public ProductionActivityStep ProductionActivityStep { get; set; }
    [StringLength(1000)] public string BatchNumber { get; set; }
    public DateTime? ManufacturingDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal BatchQuantity { get; set; }
    public BatchManufacturingStatus Status { get; set; }
    public Guid? IssuedById { get; set; }
    public User IssuedBy { get; set; }
}

public enum BatchManufacturingStatus
{
    New = 0,
    UnderTest = 1,
    Approved = 2,
    Rejected = 3,
}

public class BatchManufacturingRecordDto : BaseDto
{
    public CollectionItemDto ProductionSchedule { get; set; }
    public ProductListDto Product { get; set; }
    public string BatchNumber { get; set; }
    public DateTime? ManufacturingDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal BatchQuantity { get; set; }
    public BatchManufacturingStatus Status { get; set; }
    public decimal ExpectedQuantity => BatchQuantity / Product.BasePackingQuantity;
}