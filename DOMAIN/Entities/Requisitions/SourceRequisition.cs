using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Procurement.Suppliers;
using SHARED;

namespace DOMAIN.Entities.Requisitions;

public class SourceRequisition : BaseEntity
{
    [StringLength(100)] public string Code { get; set; }
    public Guid RequisitionId { get; set; }
    public Requisition Requisition { get; set; }
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
    public int Quantity { get; set; }
    public ProcurementSource Source { get; set; }
    public List<SourceRequisitionItemSupplier> Suppliers { get; set; } = [];

}

public enum ProcurementSource
{
    Foreign,
    Local,
    Internal
}

public class SourceRequisitionItemSupplier : BaseEntity
{
    public Guid SourceRequisitionItemId { get; set; }
    public SourceRequisitionItem SourceRequisitionItem { get; set; }
    public Guid SupplierId { get; set; }
    public Supplier Supplier { get; set; }
    public DateTime? SentQuotationRequestAt { get; set; }
}

public class SourceRequisitionDto : BaseDto
{
    public string Code { get; set; }
    public Guid RequisitionId { get; set; }
    public Requisition Requisition { get; set; }
    public List<SourceRequisitionItemDto> Items { get; set; } = [];

}

public class SourceRequisitionItemDto
{
    public Guid Id { get; set; }
    public CollectionItemDto SourceRequisition { get; set; }
    public CollectionItemDto Material { get; set; }
    public CollectionItemDto UoM { get; set; }
    public int Quantity { get; set; }
    public ProcurementSource Source { get; set; }
    public List<SourceRequisitionItemSupplierDto> Suppliers { get; set; } = [];
    public DateTime? SentQuotationRequestAt { get; set; }
    public bool SentQuotationRequest => SentQuotationRequestAt is not null;
}
public class SourceRequisitionItemSupplierDto 
{
    public CollectionItemDto Supplier { get; set; }
}