using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Shipments;

namespace DOMAIN.Entities.Procurement.Distribution;

public class MaterialDistributionDto
{
    public List<MaterialDistributionSection> Sections { get; set; } = new List<MaterialDistributionSection>();
}

public class MaterialDistributionSection
{
    public MaterialDto Material { get; set; }
    public ShipmentInvoiceDto ShipmentInvoice { get; set; }
    public List<ShipmentInvoiceItemDto> ShipmentInvoiceItems { get; set; }
    public decimal TotalQuantity { get; set; }
    public List<DistributionRequisitionItem> Items { get; set; } = new List<DistributionRequisitionItem>();
}

public class MaterialDistributionSectionRequest
{
    public List<Guid> ShipmentInvoiceItemIds { get; set; }
    public Guid? SupplierId { get; set; }
    public Guid? ManufacturerId { get; set; }
    public List<DistributionRequisitionItemRequest> Items { get; set; }
}

public class DistributionRequisitionItem
{
    public DepartmentDto Department { get; set; }
    public RequisitionItemDto RequistionItem { get; set; }
    public decimal? QuantityRequested { get; set; }
    public decimal? QuantityAllocated { get; set; }
    public decimal? QuantityRemaining { get; set; }
}

public class DistributionRequisitionItemRequest
{
    public Guid? DepartmentId { get; set; }
    public Guid? RequistionItemId { get; set; }
    public decimal QuantityAllocated { get; set; }
}