using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Users;
using SHARED;

namespace DOMAIN.Entities.Products.Production;

public class CreateBatchPackagingRecord
{
    public Guid ProductionScheduleProductId { get; set; }
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
    public Guid? ProductPackingId { get; set; }
}

public class BatchPackagingRecord : BaseEntity
{
    public Guid ProductionScheduleProductId { get; set; }
    public ProductionScheduleProduct ProductionScheduleProduct { get; set; }
    public Guid ProductionActivityStepId { get; set; }
    public ProductionActivityStep ProductionActivityStep { get; set; }
    [StringLength(100)] public string BatchNumber { get; set; }
    public DateTime? ManufacturingDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal BatchQuantity { get; set; }
    public Guid? IssuedById { get; set; }
    public User IssuedBy { get; set; }
    public Guid? ProductPackingId { get; set; }
    public ProductPacking ProductPacking { get; set; }
    public DateTime? IssuedDate { get; set; }
}

public class BatchPackagingRecordDto : BaseDto
{
    public ProductionScheduleProductDto  ProductionScheduleProductDto{get; set; }
    public string BatchNumber { get; set; }
    public DateTime? ManufacturingDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public ProductPackingDto ProductPacking { get; set; }
    public decimal BatchQuantity { get; set; }
}