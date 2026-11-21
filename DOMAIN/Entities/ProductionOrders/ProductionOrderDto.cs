using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Customers;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Products.Production;

namespace DOMAIN.Entities.ProductionOrders;

public class ProductionOrderDto : BaseDto
{
    public string Code { get; set; }
    public CustomerDto Customer { get; set; }
    public List<ProductionOrderProductsDto> Products { get; set; } = [];
    public decimal TotalValue { get; set; }
    public ProductionOrderStatus Status { get; set; }
    public bool Approved { get; set; }
    public DateTime? PromisedDeliveryDate { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public Guid? SourceCustomerQuotationId { get; set; }
}

public class ProductionOrderListDto : BaseDto
{
    public string Code { get; set; }
    public CustomerDto Customer { get; set; }
    public decimal TotalValue { get; set; }
    public ProductionOrderStatus Status { get; set; }
    public bool Approved { get; set; }
    public DateTime? PromisedDeliveryDate { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public Guid? SourceCustomerQuotationId { get; set; }
}

public class ProductionOrderDetailDto : ProductionOrderDto
{
    public ProductionOrderInvoiceDto Invoice { get; set; }
}

public class ProductionOrderProductsDto
{
    public ProductListDto Product { get; set; }
    public int TotalOrderQuantity { get; set; }
    public decimal VolumePerPiece { get; set; }
    public Guid? UoMId { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal TotalVolume { get; set; }
    public decimal TotalBatches { get; set; }
    public decimal TotalValue { get; set; }
    public List<ProductionOrderProductQuantityDto> FulfilledQuantities { get; set; } = [];
    public bool Fulfilled { get; set; }
    public decimal RemainingQuantity { get; set; }
    public ProductPackingDto ProductPacking { get; set; }
    public decimal Shippers { get; set; }
    public decimal Loose { get; set; }
}

public class ProductionOrderProductQuantityDto
{
    public decimal Quantity { get; set; }
    /// <summary>The base units this took from the transfer note's stock.</summary>
    public decimal YieldQuantity { get; set; }
    /// <summary>The units in one pack used for the conversion.</summary>
    public decimal UnitsPerPack { get; set; }
}

public class AllocateProductionOrderRequest
{
    public string FefoExceptionReason { get; set; }

    public Guid ProductionOrderId { get; set; }
    public List<AllocateProductionOrderProductRequest> Products { get; set; } = [];
}

public class AllocateProductionOrderProductRequest
{
    public Guid ProductId { get; set; }

    [Required]
    public Guid? ProductPackingId { get; set; }
    public List<AllocateProductQuantityRequest> FulfilledQuantities { get; set; } = [];
}

public class AllocateProductQuantityRequest
{
    public Guid FinishedGoodsTransferNoteId { get; set; }
    public decimal Quantity { get; set; }
}

public class AllocateProductionOrderDto
{
    public string FefoExceptionReason { get; set; }

    public DateTime? CostingConfirmedAt { get; set; }

    public string LoadedVehicle { get; set; }
    public string LoadedDriver { get; set; }
    public string LoadedDriverPhone { get; set; }
    public string LoadingLocation { get; set; }
    public string LoadingInspectionReference { get; set; }

    public DateTime? DispatchedAt { get; set; }
    public string ExitReference { get; set; }
    public DateTime? CommercialClearedAt { get; set; }
    public string CommercialClearanceReason { get; set; }
    public string DeliveryReceiver { get; set; }
    public string DeliveryEvidence { get; set; }
    public string DeliveryCondition { get; set; }
    public List<FinishedGoodsDeliveryLine> DeliveryLines { get; set; } = [];

    public Guid Id { get; set; }
    public ProductionOrderListDto ProductionOrder { get; set; }
    public string InvoiceCode { get; set; }
    public bool Approved { get; set; }
    public List<AllocateProductionOrderProductDto> Products { get; set; } = [];
    public AllocateProductionOrderStatus Status { get; set; }
    public DateTime? LoadedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool HasInvoice { get; set; }
    public bool HasWayBill { get; set; }
}

public class AllocateProductionOrderProductDto
{
    public decimal? UnitPrice { get; set; }
    public Guid? PriceCurrencyId { get; set; }
    public Guid? PriceListRevisionId { get; set; }
    public ProductListDto Product { get; set; }
    public ProductPackingDto ProductPacking { get; set; }
    public List<AllocateProductQuantityDto> FulfilledQuantities { get; set; } = [];
}

public class AllocateProductQuantityDto
{
    public FinishedGoodsTransferNoteDto FinishedGoodsTransferNote { get; set; }
    public BatchPackagingRecordDto BatchPackagingRecord { get; set; }
    public decimal Quantity { get; set; }
    /// <summary>The base units this took from the transfer note's stock.</summary>
    public decimal YieldQuantity { get; set; }
    /// <summary>The units in one pack used for the conversion.</summary>
    public decimal UnitsPerPack { get; set; }
}
