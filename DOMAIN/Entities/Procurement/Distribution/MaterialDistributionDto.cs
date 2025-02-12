using DOMAIN.Entities.Requisitions;

namespace DOMAIN.Entities.Procurement.Distribution;

public class MaterialDistributionDto
{
    public List<MaterialDistributionSection> Sections { get; set; }
}

public class MaterialDistributionSection
{
    public Guid MaterialId { get; set; }
    public string MaterialName { get; set; }
    public Guid ShipmentInvoiceItemId { get; set; }
    public decimal TotalQuantity { get; set; }
    public List<DistributionRequisitionItem> Items { get; set; }
}

public class DistributionRequisitionItem
{
    public string DepartmentName { get; set; }
    public Guid DepartmentId { get; set; }
    public Guid RequistionItemId { get; set; }
    public decimal QuantityRequested { get; set; }
    public decimal QuantityAllocated { get; set; }
    public decimal QuantityRemaining { get; set; }
}