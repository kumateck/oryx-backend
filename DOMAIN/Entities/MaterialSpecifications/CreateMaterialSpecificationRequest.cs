using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.MaterialSpecifications;

public class CreateMaterialSpecificationRequest
{
    [Required, MinLength(1)] public string SpecificationNumber { get; set; }
    [Required, MinLength(1)] public string RevisionNumber { get; set; }
    [Required, MinLength(1)] public string SupersedesNumber { get; set; }
    [Required] public DateTime EffectiveDate { get; set; }
    [Required] public DateTime ReviewDate { get; set; }
    [Required] public Guid FormId { get; set; }
    public DateTime DueDate {get;set;}
    public string Description {get;set;}
    
    [Required]public Guid UserId { get; set; }
    [Required] public Guid MaterialId { get; set; }
    
}