using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.Products;

public class CreateProductRequest
{ 
    [StringLength(255)] public string Code { get; set; } 
    [StringLength(255)] public string Name { get; set; }
    [StringLength(255)] public string Description { get; set; }
    [StringLength(255)] public string GenericName { get; set; }
    [StringLength(255)] public string StorageCondition { get; set; }
    [StringLength(255)] public string PackageStyle { get; set; }
    [StringLength(255)] public string FilledWeight { get; set; }
    [StringLength(255)] public string ShelfLife { get; set; }
    [StringLength(255)] public string ActionUse { get; set; }
    [StringLength(255)] public string FdaRegistrationNumber { get; set; }
    [StringLength(255)] public string MasterFormulaNumber { get; set; }
    public Guid CategoryId { get; set; } // e.g., Tablet, Syrup, Injectable
    public List<CreateFinishedProductRequest> FinishedProducts { get; set; }
    public decimal BaseQuantity { get; set; } 
    public Guid? BaseUomId { get; set; }
    public decimal BasePackingQuantity { get; set; } 
    public Guid? BasePackingUomId { get; set; }
}