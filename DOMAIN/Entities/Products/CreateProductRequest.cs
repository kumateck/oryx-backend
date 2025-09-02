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
    [StringLength(1000000)] public string PrimaryPackDescription { get; set; }
    [StringLength(1000000)] public string SecondaryPackDescription { get; set; }
    [StringLength(1000000)] public string TertiaryPackDescription { get; set; }
    public Guid CategoryId { get; set; } // e.g., Tablet, Syrup, Injectable
    public List<CreateFinishedProductRequest> FinishedProducts { get; set; }
    public Guid? EquipmentId { get; set; }
    public decimal BaseQuantity { get; set; } 
    public Guid? BaseUomId { get; set; }
    public decimal BasePackingQuantity { get; set; } 
    public decimal FullBatchSize { get; set; }
    public Guid? BasePackingUomId { get; set; }
    public Guid? DepartmentId { get; set; }
    public decimal Price { get; set; }
    public Division Division { get; set; }
    public int PackPerShipper { get; set; }
    public decimal ExpectedYield { get; set; }
}