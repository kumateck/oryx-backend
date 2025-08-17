using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Customers;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Products;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace DOMAIN.Entities.ProductionOrders;

public class ProductionOrder : BaseEntity
{
    [StringLength(1000)] public string Code { get; set; }
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
    public bool Fulfilled { get; set; }
    public List<ProductionOrderProductQuantity> FulfilledQuantities { get; set; } = [];
    public decimal RemainingQuantity => TotalOrderQuantity - FulfilledQuantities.Sum(p => p.Quantity);
}

[Owned]
public class ProductionOrderProductQuantity
{
    public Guid FinishedGoodsTransferNoteId {get; set;}
    public FinishedGoodsTransferNote  FinishedGoodsTransferNote { get; set; }
    public decimal Quantity {get; set;}
}