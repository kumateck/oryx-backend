using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Warehouses;

namespace DOMAIN.Entities.Reports;

public sealed class MaterialsReadyForChecklistFilter : ReportFilter
{
    /// <summary>
    /// Production department to report on. Null means all production departments for
    /// non-production users. Production users are always restricted to their own department.
    /// </summary>
    public Guid? DepartmentId { get; set; }
}

/// <summary>
/// Read-optimized report row. It intentionally excludes stock, specification, supplier,
/// checklist-batch, and other entity graphs that are not needed to identify pending receipts.
/// </summary>
public sealed class MaterialReadyForChecklistDto
{
    public Guid Id { get; set; }
    public Guid? MaterialId { get; set; }
    public string MaterialCode { get; set; }
    public string MaterialName { get; set; }
    public MaterialKind MaterialKind { get; set; }
    public Guid? ShipmentInvoiceId { get; set; }
    public string ShipmentInvoiceCode { get; set; }
    public decimal Quantity { get; set; }
    public Guid? UoMId { get; set; }
    public string UoMName { get; set; }
    public string UoMSymbol { get; set; }
    public DateTime? ArrivedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DistributedRequisitionMaterialStatus Status { get; set; }
    public Guid? DepartmentId { get; set; }
    public string DepartmentName { get; set; }
}
