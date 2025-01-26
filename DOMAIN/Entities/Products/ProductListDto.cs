using DOMAIN.Entities.Base;
using SHARED;

namespace DOMAIN.Entities.Products;

public class ProductListDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } 
    public string Name { get; set; }
    public string Description { get; set; }
    public string GenericName { get; set; }
    public string StorageCondition { get; set; }
    public string PackageStyle { get; set; }
    public string FilledWeight { get; set; }
    public string ShelfLife { get; set; }
    public string ActionUse { get; set; }
    public string FdaRegistrationNumber { get; set; }
    public string MasterFormulaNumber { get; set; }
    public CollectionItemDto Category { get; set; }
    public decimal BaseQuantity { get; set; } 
    public decimal BasePackingQuantity { get; set; } 
    public UnitOfMeasureDto BaseUoM { get; set; }
    public UnitOfMeasureDto BasePackingUoM { get; set; }
    public DateTime CreatedAt { get; set; }
}