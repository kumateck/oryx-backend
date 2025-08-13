using DOMAIN.Entities.Base;
using DOMAIN.Entities.Customers;
using DOMAIN.Entities.Products;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace DOMAIN.Entities.ProductionOrders;

public class ProductionOrder : BaseEntity
{
    public string ProductionOrderCode { get; set; }
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; }
    public List<ProductionOrderProducts> Products { get; set; } = [];
}

[Owned]
public class ProductionOrderProducts
{
    public Guid ProductId {get; set;}
    public Product Product { get; set; }
    public int TotalOrderQuantity { get; set; }
    public decimal VolumePerPiece { get; set; }
    public decimal TotalVolume => TotalOrderQuantity * VolumePerPiece;
    public decimal TotalBatches =>  Product?.FullBatchSize > 0 
        ? TotalVolume / Product.FullBatchSize 
        : 0;
    public decimal TotalValue => TotalOrderQuantity * Product.Price;
}

public class ProductionOrderProductsDto
{
    public CollectionItemDto Product { get; set; }
    public int TotalOrderQuantity { get; set; }
    public decimal VolumePerPiece { get; set; }
    public decimal TotalVolume { get; set; }
    public decimal TotalBatches { get; set; }
    public decimal TotalValue { get; set; }
}