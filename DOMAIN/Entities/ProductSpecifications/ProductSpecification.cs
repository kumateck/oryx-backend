using DOMAIN.Entities.AnalyticalTestRequests;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.ProductSpecifications;

public class ProductSpecification : BaseEntity, IVerifiable
{
    /// <summary>Product-specific, per-output reporting rules. Never copied by shared-number updates.</summary>
    public string AssayReportingRulesJson { get; set; }
    public string SpecificationNumber { get; set; }
    public string RevisionNumber { get; set; }
    public string SupersedesNumber { get; set; }
    /// <summary>The reason stated for the latest revision. Earlier reasons are in the row's history versions.</summary>
    public string RevisionReason { get; set; }
    public string LabelClaim { get; set; }
    public string PackingStyle { get; set; }
    public string ShelfLife { get; set; }
    public DateTime EffectiveDate { get; set; }
    public DateTime ReviewDate { get; set; }
    public Guid FormId { get; set; }
    public Form Form { get; set; }
    public DateTime DueDate { get; set; }
    public string Description { get; set; }
    public string Reference { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; }
    public TestStage TestStage { get; set; }
    public Guid ProductId { get; set; }
    public Product Product { get; set; }
    public Guid? ResponseId { get; set; }
    public Response Response { get; set; }
    public List<FormSection> FormSections { get; set; } = [];
    public bool IsVerified { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public Guid? VerifiedById { get; set; }
}
