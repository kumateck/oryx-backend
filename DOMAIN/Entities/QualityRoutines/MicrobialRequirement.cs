using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.AnalyticalTestRequests;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Products;

namespace DOMAIN.Entities.QualityRoutines;

public enum RequirementSubject
{
    Material = 0,
    Product = 1,
}

public class MicrobialRequirement : BaseEntity, IVerifiable
{
    public RequirementSubject Subject { get; set; }
    public Guid? MaterialId { get; set; }
    public Material Material { get; set; }
    public Guid? ProductId { get; set; }
    public Product Product { get; set; }
    public TestStage? Stage { get; set; }
    public bool Required { get; set; }
    [StringLength(4000)] public string Reason { get; set; }
    public bool IsVerified { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public Guid? VerifiedById { get; set; }
}

public class CreateMicrobialRequirementRequest
{
    public RequirementSubject Subject { get; set; }
    public Guid? MaterialId { get; set; }
    public Guid? ProductId { get; set; }
    public TestStage? Stage { get; set; }
    public bool Required { get; set; }
    [Required, StringLength(4000)] public string Reason { get; set; }
}

public class MicrobialRequirementDto
{
    public Guid Id { get; set; }
    public RequirementSubject Subject { get; set; }
    public Guid? MaterialId { get; set; }
    public Guid? ProductId { get; set; }
    public string SubjectName { get; set; }
    public TestStage? Stage { get; set; }
    public bool Required { get; set; }
    public string Reason { get; set; }
    public bool IsVerified { get; set; }
    public DateTime? VerifiedAt { get; set; }
}
