using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.Products;

public class UpdateProductRequest : CreateProductRequest
{
}

public class UpdateProductPackageDescriptionRequest
{
    [StringLength(1000000)] public string PrimaryPackDescription { get; set; }
    [StringLength(1000000)] public string SecondaryPackDescription { get; set; }
    [StringLength(1000000)] public string TertiaryPackDescription { get; set; }
}