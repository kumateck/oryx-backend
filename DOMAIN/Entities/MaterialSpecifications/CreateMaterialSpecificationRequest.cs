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
    public DateTime DueDate { get; set; }
    public string Description { get; set; }
    public string Reference { get; set; }

    [Required] public Guid UserId { get; set; }
    [Required] public List<Guid> MaterialIds { get; set; }

}

public class UpdateMaterialSpecificationRequest
{
    [Required, MinLength(1)] public string SpecificationNumber { get; set; }
    [Required, MinLength(1)] public string RevisionNumber { get; set; }
    [Required, MinLength(1)] public string SupersedesNumber { get; set; }
    [Required] public DateTime EffectiveDate { get; set; }
    [Required] public DateTime ReviewDate { get; set; }
    [Required] public Guid FormId { get; set; }
    public DateTime DueDate { get; set; }
    public string Description { get; set; }
    public string Reference { get; set; }
}

public class AddRemoveMaterialToSpecificationRequest
{
    [Required]
    public string SpecificationNumber { get; set; }
    public List<Guid> MaterialIdsToAdd { get; set; } = [];
    public List<Guid> MaterialIdsToRemove { get; set; } = [];
}