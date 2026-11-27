using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.MaterialSpecifications;
using DOMAIN.Entities.ProductSpecifications;

namespace DOMAIN.Entities.Forms;

public enum MicrobialLimitKind
{
    /// <summary>The CFU result must not exceed the value ("NMT 1000 cfu/g").</summary>
    NotMoreThan = 0,
    /// <summary>The organism must be absent from the quantity tested ("Absence of Escherichia coli in 1 g").</summary>
    Absent = 1,
}

/// <summary>
/// A typed acceptance limit for one result field of a specification's microbial template.
/// Each specification row holds its own copy; rows sharing a specification number are saved together.
/// </summary>
public class SpecificationMicrobialLimit : BaseEntity
{
    public Guid? MaterialSpecificationId { get; set; }
    public MaterialSpecification MaterialSpecification { get; set; }
    public Guid? ProductSpecificationId { get; set; }
    public ProductSpecification ProductSpecification { get; set; }
    /// <summary>The CfuCalculation (NotMoreThan) or GrowthObservation (Absent) field of the microbial template.</summary>
    public Guid FormFieldId { get; set; }
    public FormField FormField { get; set; }
    public MicrobialLimitKind Kind { get; set; }
    public decimal? Value { get; set; }
    [StringLength(20)] public string Unit { get; set; }
    [StringLength(100)] public string Organism { get; set; }
    public decimal? Quantity { get; set; }
    [StringLength(10)] public string QuantityUnit { get; set; }
    public int DisplayOrder { get; set; }
    /// <summary>The reason given when this set of limits was saved.</summary>
    [StringLength(1000)] public string Reason { get; set; }
    public Guid SavedById { get; set; }
    public DateTime SavedAt { get; set; }
}

public class MicrobialLimitRequest
{
    [Required] public Guid FormFieldId { get; set; }
    public MicrobialLimitKind Kind { get; set; }
    public decimal? Value { get; set; }
    [StringLength(20)] public string Unit { get; set; }
    [StringLength(100)] public string Organism { get; set; }
    public decimal? Quantity { get; set; }
    [StringLength(10)] public string QuantityUnit { get; set; }
}

public class SaveMicrobialLimitsRequest
{
    /// <summary>Why the limits changed; recorded and required (at least 10 characters).</summary>
    [Required, StringLength(1000)] public string Reason { get; set; }
    public List<MicrobialLimitRequest> Limits { get; set; } = [];
}

public class MicrobialLimitFieldDto
{
    public Guid FormFieldId { get; set; }
    public string SectionName { get; set; }
    public string QuestionLabel { get; set; }
    public QuestionType QuestionType { get; set; }
    /// <summary>For a CfuCalculation field, the unit its question reports in.</summary>
    public string Unit { get; set; }
}

public class MicrobialLimitDto
{
    public Guid FormFieldId { get; set; }
    public MicrobialLimitKind Kind { get; set; }
    public decimal? Value { get; set; }
    public string Unit { get; set; }
    public string Organism { get; set; }
    public decimal? Quantity { get; set; }
    public string QuantityUnit { get; set; }
    /// <summary>The limit as printed, e.g. "NMT 1000 cfu/g".</summary>
    public string Text { get; set; }
}

public class SpecificationMicrobialLimitsDto
{
    public Guid SpecificationId { get; set; }
    public Guid? MicrobialFormId { get; set; }
    /// <summary>The template's result fields a limit can be set on.</summary>
    public List<MicrobialLimitFieldDto> Fields { get; set; } = [];
    public List<MicrobialLimitDto> Limits { get; set; } = [];
    public Dictionary<string, string> Organisms { get; set; } = [];
    public string Reason { get; set; }
    public DateTime? SavedAt { get; set; }
}
