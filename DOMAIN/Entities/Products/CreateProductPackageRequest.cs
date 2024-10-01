using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.Products;

public class CreateProductPackageRequest
{
    public Guid MaterialId { get; set; }
    public Guid PackageTypeId { get; set; }
    [StringLength(255, ErrorMessage = "Should be 255 characters or less")] public string MaterialThickness { get; set; }
    [StringLength(255,  ErrorMessage = "Should be 255 characters or less")] public string OtherStandards { get; set; }
  
}