using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.Materials;

public class MaterialDto
{
    public Guid Id { get; set; }
    public string Code { get; set; }
    public string Pharmacopoeia { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Alphabet { get; set; }
    public MaterialKind Kind { get; set; }
    public MaterialCategoryDto MaterialCategory { get; set; }
    public decimal TotalStock { get; set; }
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
}

public class BatchQuantityDto
{
    public Guid ShelfMaterialBatchId { get; set; }
    public decimal Quantity { get; set; }
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
}