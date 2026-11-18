using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.MaterialSpecifications;
using DOMAIN.Entities.Procurement.Manufacturers;
using DOMAIN.Entities.Procurement.Suppliers;

namespace DOMAIN.Entities.Materials;

public class MaterialDto
{
    public decimal? DensityGramsPerMilliliter { get; set; }
    public Guid Id { get; set; }
    public string Code { get; set; }
    public string Pharmacopoeia { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Alphabet { get; set; }
    public MaterialKind Kind { get; set; }
    public bool IsUnlimited { get; set; }
    public bool IsWhitePlacebo { get; set; }
    public MaterialCategoryDto MaterialCategory { get; set; }
    public MaterialSpecificationReducedDto Specification { get; set; }
    public decimal TotalStock { get; set; }
    public decimal ReservedStock { get; set; }
    public List<SupplierListDto> Suppliers { get; set; } = [];
    public List<ManufacturerListDto> Manufacturers { get; set; } = [];
}

public class MaterialWithWarehouseStockDto : MaterialDto
{
    public decimal WarehouseStock { get; set; }
}

public class MaterialDetailsDto
{
    public MaterialDto Material { get; set; }
    public UnitOfMeasureDto UnitOfMeasure { get; set; }
    public decimal TotalAvailableQuantity { get; set; }
    public decimal ReservedQuantity { get; set; }
    /// <summary>Lots past expiry or retest, in the department's unit. Set by screens that use the store-stock rule.</summary>
    public decimal? ExpiredQuantity { get; set; }
    /// <summary>In-date lots not released for use, or whose unit cannot be read.</summary>
    public decimal? UnavailableQuantity { get; set; }
    /// <summary>Approved, in-date stock not held for a batch: what can be issued or reserved now.</summary>
    public decimal? FreeQuantity { get; set; }
}

public class BatchQuantityDto
{
    public Guid ShelfMaterialBatchId { get; set; }
    public decimal Quantity { get; set; }
}

public class MaterialWarehouseStockBreakdown
{
    public decimal WarehouseStock { get; set; }
    public decimal ExpiredQuantity { get; set; }
    public decimal OnHandQuantity { get; set; }
    public decimal UnavailableQuantity { get; set; }
    public decimal ReservedQuantity { get; set; }
    public decimal FreeQuantity { get; set; }
}

public class MaterialCategoryDto : BaseDto
{
    public string Name { get; set; }
    public string Description { get; set; }
    public MaterialKind MaterialKind { get; set; }
}

public class MaterialDepartmentWithWarehouseStockDto : MaterialDepartmentDto
{
    public decimal WarehouseStock { get; set; }
    public decimal PendingStockTransferQuantity { get; set; }
    public decimal ReservedQuantity { get; set; }
    public decimal ExpiredQuantity { get; set; }
}

public class MaterialWithStockDto
{
    public MaterialDto Material { get; set; }
    public decimal StockQuantity { get; set; }
    public UnitOfMeasureDto UoM { get; set; }
}

public class MaterialBatchDepartmentDto
{
    public MaterialDto Material { get; set; }
    public List<MaterialBatchDto> Batches { get; set; }
    public List<MaterialDepartmentDto> ProductionDepartments { get; set; }
}
