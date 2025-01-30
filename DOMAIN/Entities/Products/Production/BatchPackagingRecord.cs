using DOMAIN.Entities.Base;
using SHARED;

namespace DOMAIN.Entities.Products.Production;

public class CreateBatchPackagingRecord
{
    public Guid ProductId { get; set; }
    public string BatchNumber { get; set; }
    public DateTime ManufacturingDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public decimal BatchQuantity { get; set; }
}

public class BatchPackagingRecord : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; }
    public string BatchNumber { get; set; }
    public DateTime ManufacturingDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public decimal BatchQuantity { get; set; }
}

public class BatchPackagingRecordDto : BaseDto
{
    public CollectionItemDto Product { get; set; }
    public string BatchNumber { get; set; }
    public DateTime ManufacturingDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public decimal BatchQuantity { get; set; }
}