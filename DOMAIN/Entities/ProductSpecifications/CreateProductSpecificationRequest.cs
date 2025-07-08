using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.AnalyticalTestRequests;
using DOMAIN.Entities.MaterialSpecifications;

namespace DOMAIN.Entities.ProductSpecifications;

public class CreateProductSpecificationRequest
{   
    [Required, MinLength(1)] public string SpecificationNumber { get; set; }
    [Required, MinLength(1)] public string RevisionNumber { get; set; }
    [Required, MinLength(1)] public string SupersedesNumber { get; set; }
    
    [Required] public DateTime EffectiveDate { get; set; }
    [Required] public DateTime ReviewDate { get; set; }
    [Required] public List<TestSpecification> TestSpecifications { get; set; } = [];
    [Required] public TestStage TestStage { get; set; }
    [Required] public Guid ProductId { get; set; }
    
}