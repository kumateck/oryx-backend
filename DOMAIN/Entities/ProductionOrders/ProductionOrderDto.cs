using DOMAIN.Entities.Base;
using DOMAIN.Entities.Customers;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Products;
using SHARED;

namespace DOMAIN.Entities.ProductionOrders;

public class ProductionOrderDto : BaseDto
{
    public string Code { get; set; }
    public CustomerDto Customer { get; set; }
    public List<ProductionOrderProductsDto> Products { get; set; } = [];
    public decimal TotalValue { get; set; }
}

public class ProductionOrderProductsDto
{
    public ProductListDto Product { get; set; }
    public int TotalOrderQuantity { get; set; }
    public decimal VolumePerPiece { get; set; }
    public decimal TotalVolume { get; set; }
    public decimal TotalBatches { get; set; }
    public decimal TotalValue { get; set; }
    public List<ProductionOrderProductQuantityDto>  FulfilledQuantities { get; set; } = [];
    public bool Fulfilled { get; set; }
}

public class ProductionOrderProductQuantityDto
{
    public decimal Quantity { get; set; }
}

public class AllocateProductionOrderRequest
{
    public Guid ProductionOrderId { get; set; }
    public List<AllocateProductionOrderProductRequest> Products { get; set; } = [];
    
}

public class AllocateProductionOrderProductRequest
{
    public Guid ProductId { get; set; }
    public List<AllocateProductQuantityRequest> FulfilledQuantities { get; set; } = [];
}

public class AllocateProductQuantityRequest
{
    public Guid FinishedGoodsTransferNoteId { get; set; }
    public decimal Quantity { get; set; }
}


public class AllocateProductionOrderDto : BaseDto
{
    public ProductionOrderDto ProductionOrder { get; set; }
    public bool Approved { get; set; }
    public List<AllocateProductionOrderProductDto> Products { get; set; } = [];
    public DateTime? DeliveredAt { get; set; }
    
}

public class AllocateProductionOrderProductDto
{
    public CollectionItemDto Product { get; set; }
    public List<AllocateProductQuantityDto> FulfilledQuantities { get; set; } = [];
}

public class AllocateProductQuantityDto
{
    public FinishedGoodsTransferNoteDto FinishedGoodsTransferNote { get; set; }
    public decimal Quantity { get; set; }
}