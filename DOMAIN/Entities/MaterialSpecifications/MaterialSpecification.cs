using DOMAIN.Entities.Base;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.MaterialSpecifications;

public class MaterialSpecification : BaseEntity, IVerifiable
{
    public string SpecificationNumber { get; set; }
    public string RevisionNumber { get; set; }
    public string SupersedesNumber { get; set; }
    /// <summary>The reason stated for the latest revision. Earlier reasons are in the row's history versions.</summary>
    public string RevisionReason { get; set; }
    public DateTime EffectiveDate { get; set; }
    public DateTime ReviewDate { get; set; }
    public Guid FormId { get; set; }
    public Form Form { get; set; }
    /// <summary>The microbial worksheet template. Its sections carry the microbial tests and their references; FormId stays the chemical template.</summary>
    public Guid? MicrobialFormId { get; set; }
    public Form MicrobialForm { get; set; }
    public DateTime DueDate { get; set; }
    public string Description { get; set; }
    public string Reference { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; }
    public Guid MaterialId { get; set; }
    public Material Material { get; set; }
    public Guid? ResponseId { get; set; }
    public Response Response { get; set; }
    public List<FormSection> FormSections { get; set; } = [];
    public bool IsVerified { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public Guid? VerifiedById { get; set; }
}



public enum MaterialSpecificationReference
{
    BP,
    USP,
    PhInt,
    InHouse
}
