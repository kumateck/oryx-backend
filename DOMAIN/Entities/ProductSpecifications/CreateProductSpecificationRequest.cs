using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.AnalyticalTestRequests;

namespace DOMAIN.Entities.ProductSpecifications;

public class CreateProductSpecificationRequest
{
    [Required, MinLength(1)] public string SpecificationNumber { get; set; }
    [Required, MinLength(1)] public string RevisionNumber { get; set; }
    [Required, MinLength(1)] public string SupersedesNumber { get; set; }

    [Required] public DateTime EffectiveDate { get; set; }
    [Required] public DateTime ReviewDate { get; set; }
    [Required] public Guid FormId { get; set; }
    /// <summary>Optional microbial template; must differ from FormId.</summary>
    public Guid? MicrobialFormId { get; set; }

    [Required] public Guid UserId { get; set; }

    [Required] public TestStage TestStage { get; set; }
    [Required] public List<Guid> ProductIds { get; set; }

    public DateTime DueDate { get; set; }
    public string Description { get; set; }
    public string Reference { get; set; }
}

public class UpdateProductSpecificationRequest
{
    /// <summary>Why this revision was made. Required on every edit, at least 10 characters; recorded in the revision history.</summary>
    [Required, StringLength(1000)] public string RevisionReason { get; set; }
    [Required, MinLength(1)] public string SpecificationNumber { get; set; }
    [Required, MinLength(1)] public string RevisionNumber { get; set; }
    [Required, MinLength(1)] public string SupersedesNumber { get; set; }

    [Required] public DateTime EffectiveDate { get; set; }
    [Required] public DateTime ReviewDate { get; set; }
    [Required] public Guid FormId { get; set; }
    /// <summary>Optional microbial template; must differ from FormId.</summary>
    public Guid? MicrobialFormId { get; set; }

    [Required] public TestStage TestStage { get; set; }

    public DateTime DueDate { get; set; }
    public string Description { get; set; }
    public string Reference { get; set; }
}

public class AddRemoveProductToSpecificationRequest
{
    [Required]
    public string SpecificationNumber { get; set; }
    public List<Guid> ProductIdsToAdd { get; set; } = [];
    public List<Guid> ProductIdsToRemove { get; set; } = [];
}