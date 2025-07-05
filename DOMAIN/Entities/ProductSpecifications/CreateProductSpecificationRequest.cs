using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.ProductSpecifications;

public class CreateProductSpecificationRequest
{
    [Required] public Guid ProductId { get; set; }
    
}