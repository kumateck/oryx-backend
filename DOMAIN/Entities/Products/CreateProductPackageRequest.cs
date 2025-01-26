using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.Products;

public class CreateProductPackageRequest
{
    [Required]
    public Guid MaterialId { get; set; }
    [Required]
    public Guid PackageTypeId { get; set; }
    [StringLength(255, ErrorMessage = "Should be 255 characters or less")] public string MaterialThickness { get; set; }
    [StringLength(255,  ErrorMessage = "Should be 255 characters or less")] public string OtherStandards { get; set; }
    public Guid? UoMId { get; set; }
    public decimal BaseQuantity { get; set; } 
    public Guid? BaseUoMId { get; set; } 
    public decimal UnitCapacity { get; set; } 
    public Guid? DirectLinkMaterialId { get; set; }
}