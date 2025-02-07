using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Users;
using SHARED;

namespace DOMAIN.Entities.Products.Production;

public class CreateBatchPackagingRecord
{
    public Guid ProductId { get; set; }
    public Guid ProductionScheduleId { get; set; }
    public Guid ProductionActivityStepId { get; set; }
    public string BatchNumber { get; set; }
    public DateTime? ManufacturingDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal BatchQuantity { get; set; }
}

public class UpdateBatchPackagingRecord
{
    public string BatchNumber { get; set; }
    public DateTime? ManufacturingDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal BatchQuantity { get; set; }
}

public class BatchPackagingRecord : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; }
    public Guid ProductionScheduleId { get; set; }
    public ProductionSchedule ProductionSchedule { get; set; }
    public Guid ProductionActivityStepId { get; set; }
    public ProductionActivityStep ProductionActivityStep { get; set; }
    [StringLength(100) ]public string BatchNumber { get; set; }
    public DateTime? ManufacturingDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal BatchQuantity { get; set; }
    public Guid? IssuedById { get; set; }
    public User IssuedBy { get; set; }
}

public class BatchPackagingRecordDto : BaseDto
{
    public CollectionItemDto ProductionSchedule { get; set; }
    public CollectionItemDto Product { get; set; }
    public string BatchNumber { get; set; }
    public DateTime? ManufacturingDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal BatchQuantity { get; set; }
}