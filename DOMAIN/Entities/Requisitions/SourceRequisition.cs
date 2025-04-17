using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Attachments;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Procurement.Suppliers;
using DOMAIN.Entities.PurchaseOrders.Request;
using SHARED;

namespace DOMAIN.Entities.Requisitions;

public class SourceRequisition : BaseEntity
{
    [StringLength(100)] public string Code { get; set; }
    public Guid SupplierId { get; set; }
    public Supplier Supplier { get; set; }
    public DateTime? SentQuotationRequestAt { get; set; }
    public List<SourceRequisitionItem> Items { get; set; } = [];
}

public class SourceRequisitionItem : BaseEntity
{
    public Guid SourceRequisitionId { get; set; }
    public SourceRequisition SourceRequisition { get; set; }
    public Guid MaterialId {  get; set; }
    public Material Material { get; set; }
    public Guid UoMId { get; set; }
    public UnitOfMeasure UoM { get; set; }
    public decimal Quantity { get; set; }
    public ProcurementSource Source { get; set; }
    public Guid RequisitionId { get; set; }
}

public enum ProcurementSource
{
    Foreign,
    Local
}


public class SourceRequisitionDto :  WithAttachment
{
    public string Code { get; set; }
    public CollectionItemDto Supplier { get; set; }
    public List<SourceRequisitionItemDto> Items { get; set; } = [];
}

public class SupplierQuotation : BaseEntity
{ 
    public Guid SupplierId { get; set; }
    public Supplier Supplier { get; set; } 
    public Guid SourceRequisitionId { get; set; }
    public SourceRequisition SourceRequisition { get; set; }
    public List<SupplierQuotationItem> Items { get; set; } = [];
    public bool ReceivedQuotation { get; set; }
}

public class SupplierQuotationDto 
{ 
    public Guid Id { get; set; }
    public SupplierDto Supplier { get; set; } 
    public List<SupplierQuotationItemDto> Items { get; set; } = [];
    public bool ReceivedQuotation { get; set; }
}

public class SupplierQuotationItem : BaseEntity
{
    public Guid SupplierQuotationId { get; set; }    
    public SupplierQuotation SupplierQuotation { get; set; }
    public Guid MaterialId { get; set; }
    public Material Material { get; set; }
    public Guid UoMId { get; set; }
    public UnitOfMeasure UoM { get; set; }
    public decimal Quantity { get; set; }
    public decimal? QuotedPrice { get; set; }
    public SupplierQuotationItemStatus Status { get; set; }
    public Guid? PurchaseOrderId { get; set; }
}

public enum SupplierQuotationItemStatus
{
    NotProcessed,
    Processed,
    NotUsed
}

public class SupplierQuotationItemDto
{
    public Guid Id { get; set; }
    public CollectionItemDto Material { get; set; }
    public UnitOfMeasureDto UoM { get; set; }
    public decimal Quantity { get; set; }
    public decimal? QuotedPrice { get; set; }
}

public class SourceRequisitionItemDto
{
    public Guid Id { get; set; }
    public CollectionItemDto SourceRequisition { get; set; }
    public MaterialDto Material { get; set; }
    public UnitOfMeasureDto UoM { get; set; }
    public decimal Quantity { get; set; }
    public ProcurementSource Source { get; set; }
    public DateTime CreatedAt { get; set; }
}
public class SupplierQuotationRequest
{
    public SupplierDto Supplier { get; set; }
    public DateTime? SentQuotationRequestAt { get; set; }
    public bool SentQuotationRequest => SentQuotationRequestAt is not null;
    public List<SourceRequisitionItemDto> Items { get; set; } = [];
}


public class SupplierQuotationResponseDto
{
    public Guid Id { get; set; }
    public decimal Price { get; set; }
}

public class SupplierPriceComparison
{
    public CollectionItemDto Material { get; set; }
    public UnitOfMeasureDto UoM { get; set; }
    public decimal Quantity { get; set; }
    public List<SupplierPrice> SupplierQuotation { get; set; } = [];
}

public class SupplierPrice
{
    public CollectionItemDto Supplier { get; set; }
    public CollectionItemDto SourceRequisition { get; set; }
    public decimal? Price { get; set; }
    
}

public class ProcessQuotation
{
    public Guid SupplierId { get; set; }
    public Guid SourceRequisitionId { get; set; }
    public List<CreatePurchaseOrderItemRequest> Items { get; set; } = [];
}