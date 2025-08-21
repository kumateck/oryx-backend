using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Customers;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Products;
using Microsoft.EntityFrameworkCore;

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
    public decimal TotalValue => TotalOrderQuantity * Product?.Price ?? 0;
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


public class AllocateProductionOrder : BaseEntity
{
    public Guid ProductionOrderId { get; set; }
    public ProductionOrder ProductionOrder { get; set; }
    public List<AllocateProductionOrderProduct> Products { get; set; } = [];
    public bool Approved { get; set; }
    public List<AllocateProductionOrderApprovals> Approvals { get; set; } = [];
    public DateTime? DeliveredAt { get; set; }
}

[Owned]
public class AllocateProductionOrderProduct
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; }
    public List<AllocateProductQuantity> FulfilledQuantities { get; set; } = [];
}

[Owned]
public class AllocateProductQuantity
{
    public Guid FinishedGoodsTransferNoteId { get; set; }
    public FinishedGoodsTransferNote  FinishedGoodsTransferNote { get; set; }
    public decimal Quantity { get; set; }
}

public class AllocateProductionOrderApprovals : ResponsibleApprovalStage
{
    public Guid Id { get; set; }
    public Guid AllocateProductionOrderId { get; set; }
    public AllocateProductionOrder AllocateProductionOrder { get; set; }
    public Guid ApprovalId { get; set; }
    public Approval Approval { get; set; }
}