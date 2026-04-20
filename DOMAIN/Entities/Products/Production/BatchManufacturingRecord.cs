using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.Products.Production;

public class CreateBatchManufacturingRecord
{
    public Guid ProductionScheduleProductId { get; set; }
    public Guid ProductionActivityStepId { get; set; }

    [Required]
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
    public Guid? ProductPackingId { get; set; }
}

public class BatchManufacturingRecord : BaseEntity
{
    public Guid ProductionScheduleProductId { get; set; }
    public ProductionScheduleProduct ProductionScheduleProduct { get; set; }
    public Guid ProductionActivityStepId { get; set; }
    public ProductionActivityStep ProductionActivityStep { get; set; }

    [StringLength(1000)]
    public string BatchNumber { get; set; }
    public DateTime? ManufacturingDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal BatchQuantity { get; set; }
    public decimal SampledQuantity { get; set; }
    public BatchManufacturingStatus Status { get; set; }
    public Guid? IssuedById { get; set; }
    public User IssuedBy { get; set; }
    public DateTime? IssuedDate { get; set; }
}

public enum BatchManufacturingStatus
{
    New = 0,
    Testing = 1,
    Approved = 2,
    Rejected = 3,
    TestTaken = 4,
    Checked = 5,
}

public class BatchManufacturingRecordDto : BaseDto
{
    public ProductionScheduleProductDto ProductionScheduleProduct { get; set; }
    public string BatchNumber { get; set; }
    public DateTime? ManufacturingDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal BatchQuantity { get; set; }
    public BatchManufacturingStatus Status { get; set; }
    public decimal SampledQuantity { get; set; }
    public DateTime? IssuedDate { get; set; }
}
