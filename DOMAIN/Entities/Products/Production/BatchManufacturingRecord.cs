using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using SHARED;

namespace DOMAIN.Entities.Products.Production;

public class CreateBatchManufacturingRecord
{
    public Guid ProductId { get; set; }
    public string BatchNumber { get; set; }
    public DateTime ManufacturingDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public decimal BatchQuantity { get; set; }
}

public class UpdateBatchManufacturingRecord
{ 
    public DateTime ManufacturingDate { get; set; } 
    public DateTime ExpiryDate { get; set; } 
    public decimal BatchQuantity { get; set; }
}

public class BatchManufacturingRecord : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; }
    [StringLength(1000)] public string BatchNumber { get; set; }
    public DateTime ManufacturingDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public decimal BatchQuantity { get; set; }
}

public class BatchManufacturingRecordDto : BaseDto
{
    public CollectionItemDto Product { get; set; }
    public string BatchNumber { get; set; }
    public DateTime ManufacturingDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public decimal BatchQuantity { get; set; }
}