#nullable enable
using DOMAIN.Entities.Materials;

namespace DOMAIN.Entities.Materials.Batch;

public class MaterialStockReportFilter
{
    public Guid? DepartmentId { get; set; }
    public MaterialKind? MaterialKind { get; set; }
    public bool ExpiredOnly { get; set; }
}

public class MaterialStockReportDto
{
    public DateTime GeneratedAt { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public MaterialKind? MaterialKind { get; set; }
    public bool ExpiredOnly { get; set; }
    public List<MaterialStockReportRow> Rows { get; set; } = [];
}

public class MaterialStockReportRow
{
    public Guid BatchId { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public Guid UoMId { get; set; }
    public string? MaterialName { get; set; }
    public string? MaterialCode { get; set; }
    public string? BatchNumber { get; set; }
    public string? GrnNumber { get; set; }
    public string? ArNumber { get; set; }
    public string? ManufacturerName { get; set; }
    public string? SupplierName { get; set; }
    public DateTime? ManufacturingDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public DateTime? RetestDate { get; set; }
    public decimal Quantity { get; set; }
    public string? UoM { get; set; }
}
