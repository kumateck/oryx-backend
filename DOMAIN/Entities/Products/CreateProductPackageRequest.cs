using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.Products;

public class CreateProductPackageRequest
{
    [Required]
    public Guid MaterialId { get; set; }
    [StringLength(255, ErrorMessage = "Should be 255 characters or less")] public string MaterialThickness { get; set; }
    [StringLength(255, ErrorMessage = "Should be 255 characters or less")] public string OtherStandards { get; set; }
    public decimal BaseQuantity { get; set; }
    public Guid? BaseUoMId { get; set; }
    public decimal UnitCapacity { get; set; }
    public Guid? DirectLinkMaterialId { get; set; }
    public decimal PackingExcessMargin { get; set; }
    public decimal PrescribedQuantity { get; set; }
    public decimal Loose { get; set; }
    public Guid? ProductPackingId { get; set; }
    public List<CreateProductPackageSubstituteRequest> Substitutes { get; set; } = [];
    public List<Guid> SubstituteMaterialIds { get; set; } = [];
}
public sealed class CreateProductPackageSubstituteRequest
{
    public Guid MaterialId { get; set; }
    [Range(typeof(decimal), "0.0000000000000000000000000001", "79228162514264337593543950335")]
    public decimal PrescribedQuantity { get; set; }
}
