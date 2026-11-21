using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Customers;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Products;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace DOMAIN.Entities.ProductionOrders;

public class ProductionOrder : BaseEntity, IRequireApproval
{
    [StringLength(1000)] public string Code { get; set; }
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; }
    public List<ProductionOrderProducts> Products { get; set; } = [];
    public ProductionOrderStatus Status { get; set; }
    public List<ProductionOrderApprovals> Approvals { get; set; } = [];
    public bool Approved { get; set; }
    public DateTime? PromisedDeliveryDate { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public Guid? SourceCustomerQuotationId { get; set; }
    public CustomerQuotation SourceCustomerQuotation { get; set; }
}

public enum ProductionOrderStatus
{
    Pending = 0,
    PartialPackingReady = 1,
    FullPackingReady = 2,
}

public class ProductionOrderApprovals : ResponsibleApprovalStage
{
    public Guid Id { get; set; }
    public Guid ProductionOrderId { get; set; }
    public ProductionOrder ProductionOrder { get; set; }
    public Guid ApprovalId { get; set; }
    public Approval Approval { get; set; }
}

[Owned]
public class ProductionOrderProducts
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; }
    public int TotalOrderQuantity { get; set; }
    public decimal VolumePerPiece { get; set; }
    public Guid? UoMId { get; set; }
    public UnitOfMeasure UoM { get; set; }
    public decimal? UnitPrice { get; set; }
    public Guid? PriceListRevisionId { get; set; }
    public Guid? PriceCurrencyId { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal TotalVolume => TotalOrderQuantity * VolumePerPiece;
    public decimal TotalBatches => Product?.FullBatchSize > 0
        ? TotalVolume / Product.FullBatchSize
        : 0;
    public decimal TotalValue => TotalOrderQuantity * (UnitPrice ?? Product?.Price ?? 0)
                                 * (1 - DiscountPercent / 100m);
    public bool Fulfilled { get; set; }
    public List<ProductionOrderProductQuantity> FulfilledQuantities { get; set; } = [];
    public decimal RemainingQuantity => TotalOrderQuantity
                                        - FulfilledQuantities.Sum(p => p.Quantity);
    public Guid? ProductPackingId { get; set; }
    public ProductPacking ProductPacking { get; set; }
    public decimal Shippers { get; set; }
    public decimal Loose { get; set; }
}

/// <summary>
/// Packs of an order line taken from one transfer note. <see cref="Quantity"/> is packs, the order's
/// unit; <see cref="YieldQuantity"/> is the base units taken out of the note's stock, worked out with
/// <see cref="UnitsPerPack"/> as it was when the packs were allocated.
/// </summary>
[Owned]
public class ProductionOrderProductQuantity
{
    public Guid FinishedGoodsTransferNoteId { get; set; }
    public FinishedGoodsTransferNote FinishedGoodsTransferNote { get; set; }
    public decimal Quantity { get; set; }
    public decimal YieldQuantity { get; set; }
    public decimal UnitsPerPack { get; set; }
}


public class AllocateProductionOrder : BaseEntity, IRequireApproval
{
    public DateTime? CostingConfirmedAt { get; set; }
    public Guid? CostingConfirmedById { get; set; }
    public List<FinishedGoodsCostingLine> CostingLines { get; set; } = [];

    public string LoadedVehicle { get; set; }
    public string LoadedDriver { get; set; }
    public string LoadedDriverPhone { get; set; }
    public string LoadingLocation { get; set; }
    public string LoadingInspectionReference { get; set; }

    public string CancellationReason { get; set; }
    public DateTime? CancelledAt { get; set; }
    public Guid? CancelledById { get; set; }
    public Guid ProductionOrderId { get; set; }
    public ProductionOrder ProductionOrder { get; set; }
    public List<AllocateProductionOrderProduct> Products { get; set; } = [];
    public bool Approved { get; set; }
    public List<AllocateProductionOrderApprovals> Approvals { get; set; } = [];
    public AllocateProductionOrderStatus Status { get; set; }
    public DateTime? LoadedAt { get; set; }
    public DateTime? WaybillSentToCustomerAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? DispatchedAt { get; set; }
    public Guid? DispatchedById { get; set; }
    public string FefoExceptionReason { get; set; }
    public string CustomerAuthorisationReference { get; set; }
    public int MinimumRemainingShelfLifeDays { get; set; }
    public string ExitReference { get; set; }
    public DateTime? CommercialClearedAt { get; set; }
    public Guid? CommercialClearedById { get; set; }
    public string CommercialClearanceReason { get; set; }
    public string DeliveryReceiver { get; set; }
    public string DeliveryEvidence { get; set; }
    public string DeliveryCondition { get; set; }
    public List<FinishedGoodsDeliveryLine> DeliveryLines { get; set; } = [];

}

public enum AllocateProductionOrderStatus
{
    Pending = 0,
    Loaded = 1,
    WaybillSentToCustomer = 2,
    Delivered = 3,
    Dispatched = 4,
    Cancelled = 5,
}

[Owned]
public class AllocateProductionOrderProduct
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; }
    public Guid? ProductPackingId { get; set; }
    public ProductPacking ProductPacking { get; set; }
    public List<AllocateProductQuantity> FulfilledQuantities { get; set; } = [];
}

/// <summary>
/// Packs a packing list takes from one transfer note. <see cref="Quantity"/> is packs, the order's
/// unit; <see cref="YieldQuantity"/> is the base units it takes from the note's stock, worked out with
/// <see cref="UnitsPerPack"/> when the packing list was created.
/// </summary>
[Owned]
public class AllocateProductQuantity
{
    public Guid FinishedGoodsTransferNoteId { get; set; }
    public FinishedGoodsTransferNote FinishedGoodsTransferNote { get; set; }
    public decimal Quantity { get; set; }
    public decimal YieldQuantity { get; set; }
    public decimal UnitsPerPack { get; set; }
}

public class AllocateProductionOrderApprovals : ResponsibleApprovalStage
{
    public Guid Id { get; set; }
    public Guid AllocateProductionOrderId { get; set; }
    public AllocateProductionOrder AllocateProductionOrder { get; set; }
    public Guid ApprovalId { get; set; }
    public Approval Approval { get; set; }
}

public class CreateProductionOrderInvoice
{
    public Guid ProductionOrderId { get; set; }
    public List<CreateProductionOrderInvoiceItem> Items { get; set; } = [];
}

public class CreateProductionOrderInvoiceItem
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}

public class ProductionOrderInvoice : BaseEntity
{
    public Guid ProductionOrderId { get; set; }
    public ProductionOrder ProductionOrder { get; set; }
    public List<ProductionOrderInvoiceItem> Items { get; set; } = [];
}

public class ProductionOrderInvoiceItem : BaseEntity
{
    public Guid ProductionOrderInvoiceId { get; set; }
    public ProductionOrderInvoice ProductionOrderInvoice { get; set; }
    public Guid ProductId { get; set; }
    public Product Product { get; set; }
    public int Quantity { get; set; }
}

public class ProductionOrderInvoiceDto : BaseDto
{
    public CollectionItemDto ProductionOrder { get; set; }
    public List<ProductionOrderInvoiceItemDto> Items { get; set; } = [];
}

public class ProductionOrderInvoiceItemDto : BaseDto
{
    public ProductListDto Product { get; set; }
    public int Quantity { get; set; }
}

public class CreateProductionOrderWaybill
{
    public Guid InvoiceId { get; set; }
    public string VehicleRegistration { get; set; }
    public string DriverName { get; set; }
    public string DriverPhone { get; set; }
    public string Transporter { get; set; }
    public string DeliveryAddress { get; set; }
    public string StorageConditions { get; set; }
    public string SealNumber { get; set; }

    [StringLength(1000, ErrorMessage = "Code character length must be less than 1000")]
    public string Code { get; set; }
    public string Comment { get; set; }
}

public class ProductionOrderWaybill : BaseEntity
{
    public bool Voided { get; set; }
    public string VoidReason { get; set; }

    public Guid InvoiceId { get; set; }
    public string VehicleRegistration { get; set; }
    public string DriverName { get; set; }
    public string DriverPhone { get; set; }
    public string Transporter { get; set; }
    public string DeliveryAddress { get; set; }
    public string StorageConditions { get; set; }
    public string SealNumber { get; set; }

    [StringLength(1000)] public string Code { get; set; }
    public Guid AllocateProductionOrderId { get; set; }
    public AllocateProductionOrder AllocateProductionOrder { get; set; }
    [StringLength(10000)] public string Comment { get; set; }
}

public class ProductionOrderWaybillDto
{
    public Guid InvoiceId { get; set; }
    public string VehicleRegistration { get; set; }
    public string DriverName { get; set; }
    public string DriverPhone { get; set; }
    public string Transporter { get; set; }
    public string DeliveryAddress { get; set; }
    public string StorageConditions { get; set; }
    public string SealNumber { get; set; }

    public Guid Id { get; set; }
    public string Code { get; set; }
    public AllocateProductionOrderDto AllocateProductionOrder { get; set; }
    [StringLength(10000)] public string Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}

[Owned]
public class FinishedGoodsDeliveryLine
{
    public Guid FinishedGoodsTransferNoteId { get; set; }
    public decimal AcceptedYield { get; set; }
    public decimal DamagedYield { get; set; }
    public decimal ShortYield { get; set; }
}
public class DispatchFinishedGoodsRequest
{
    public string ExitReference { get; set; }
    public bool VehicleInspected { get; set; }
    public bool ConditionsConfirmed { get; set; }
}
public class DeliverFinishedGoodsRequest
{
    public string ReceiverName { get; set; }
    public string EvidenceReference { get; set; }
    public string ConditionNotes { get; set; }
    public List<FinishedGoodsDeliveryLine> Lines { get; set; } = [];
}

public class ClearFinishedGoodsDispatchRequest
{
    public string Reason { get; set; }
    public bool PaymentOrCreditConfirmed { get; set; }
    public string CustomerAuthorisationReference { get; set; }
    public int MinimumRemainingShelfLifeDays { get; set; }
}

public class LoadFinishedGoodsRequest
{
    public string VehicleRegistration { get; set; }
    public string DriverName { get; set; }
    public string DriverPhone { get; set; }
    public string LoadingLocation { get; set; }
    public string InspectionReference { get; set; }
    public bool ConditionsConfirmed { get; set; }
}

[Owned]
public class FinishedGoodsCostingLine
{
    public Guid ProductId { get; set; }
    public Guid ProductPackingId { get; set; }
    public decimal ManufacturingCostPerPack { get; set; }
    public Guid CurrencyId { get; set; }
    public string ApprovedCostBasisReference { get; set; }
}
public class ConfirmFinishedGoodsCostingRequest
{
    public bool ApprovedBasisConfirmed { get; set; }
    public List<FinishedGoodsCostingLine> Lines { get; set; } = [];
}
