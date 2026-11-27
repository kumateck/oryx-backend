using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.ProductSpecifications;
using DOMAIN.Entities.QualityRoutines;

namespace DOMAIN.Entities.Forms;

/// <summary>
/// A product specification's override of a template field's "Report on COA" choice.
/// No row means the template's choice applies. Rows sharing a specification number are saved together.
/// </summary>
public class SpecificationCoaReporting : BaseEntity
{
    /// <summary>The product specification the override belongs to; exactly one of this and <see cref="MaterialSpecificationId"/> is set.</summary>
    public Guid? ProductSpecificationId { get; set; }
    public ProductSpecification ProductSpecification { get; set; }
    public Guid? MaterialSpecificationId { get; set; }
    public DOMAIN.Entities.MaterialSpecifications.MaterialSpecification MaterialSpecification { get; set; }
    public Guid FormFieldId { get; set; }
    public FormField FormField { get; set; }
    public bool ReportOnCoa { get; set; }
    /// <summary>The reason given when this set of overrides was saved.</summary>
    [StringLength(1000)] public string Reason { get; set; }
    public Guid SavedById { get; set; }
    public DateTime SavedAt { get; set; }
}

public class CoaReportingOverrideRequest
{
    [Required] public Guid FormFieldId { get; set; }
    public bool ReportOnCoa { get; set; }
}

public class SaveCoaReportingRequest
{
    /// <summary>Why the reported tests changed; recorded and required (at least 10 characters).</summary>
    [Required, StringLength(1000)] public string Reason { get; set; }
    /// <summary>Only fields whose choice differs from the template. A field left out follows the template.</summary>
    public List<CoaReportingOverrideRequest> Overrides { get; set; } = [];
}

public class CoaReportingFieldDto
{
    public Guid FormFieldId { get; set; }
    public Guid FormSectionId { get; set; }
    public string SectionName { get; set; }
    public string QuestionLabel { get; set; }
    public QuestionType QuestionType { get; set; }
    /// <summary>The template's choice.</summary>
    public bool TemplateReportOnCoa { get; set; }
    /// <summary>The specification's override, or null when it follows the template.</summary>
    public bool? Override { get; set; }
    /// <summary>What the certificate uses: the override when set, else the template's choice.</summary>
    public bool ReportOnCoa { get; set; }
}

public class SpecificationCoaReportingDto
{
    public Guid SpecificationId { get; set; }
    public AnalysisType AnalysisType { get; set; }
    /// <summary>The specification's template for this analysis type; null when none is set.</summary>
    public Guid? FormId { get; set; }
    public List<CoaReportingFieldDto> Fields { get; set; } = [];
    public string Reason { get; set; }
    public DateTime? SavedAt { get; set; }
}
