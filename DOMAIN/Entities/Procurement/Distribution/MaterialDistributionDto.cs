using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Shipments;

namespace DOMAIN.Entities.Procurement.Distribution;

public class MaterialDistributionDto
{
    public List<MaterialDistributionSection> Sections { get; set; } = [];
}

public class MaterialDistributionSection
{
    public MaterialDto Material { get; set; }
    public decimal TotalQuantity { get; set; }
    public UnitOfMeasureDto UoM { get; set; }
    public List<DistributionRequisitionItem> Items { get; set; } = [];
}

public class MaterialDistributionSectionRequest
{
    public List<Guid> ShipmentInvoiceItemIds { get; set; } = [];
    public Guid? SupplierId { get; set; }
    public List<Guid> ManufacturerIds { get; set; } = [];
    public List<DistributionRequisitionItemRequest> Items { get; set; } = [];
}

public class DistributionRequisitionItem
{
    public DepartmentDto Department { get; set; }
    public RequisitionItemDto RequisitionItem { get; set; }
    public decimal QuantityRequested { get; set; }
    public decimal QuantityAllocated { get; set; }
    public decimal QuantityRemaining { get; set; }
    public List<MaterialItemDistributionDto> Distributions { get; set; } = [];
}


public class MaterialItemDistributionDto
{
    public ShipmentInvoiceItemDto ShipmentInvoiceItem { get; set; }
    public decimal Quantity { get; set; }
}

public class DistributionRequisitionItemRequest
{
    public Guid? DepartmentId { get; set; }
    public Guid? RequisitionItemId { get; set; }
    public decimal QuantityAllocated { get; set; }
}