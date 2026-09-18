using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.QcWorksheets;

// ---------------------------------------------------------------------------
// SamplingPointGroup
// ---------------------------------------------------------------------------

public class SamplingPointGroupDto : BaseDto
{
    public string Name { get; set; }
    public string Description { get; set; }
}

public class CreateSamplingPointGroupRequest
{
    [Required, StringLength(200)] public string Name { get; set; }
    public string Description { get; set; }
}

public class UpdateSamplingPointGroupRequest : CreateSamplingPointGroupRequest;

// ---------------------------------------------------------------------------
// Specification
// ---------------------------------------------------------------------------

public class SpecificationSummaryDto : BaseDto
{
    public string Code { get; set; }
    public string Name { get; set; }
    public SpecificationAppliesTo AppliesTo { get; set; }
    public SpecificationStage? Stage { get; set; }
    public int Version { get; set; }
    public QcDocumentStatus Status { get; set; }
    public bool Approved { get; set; }
    public QcRetestPolicy RetestPolicy { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public Guid? SupersedesId { get; set; }
}

public class SpecificationDetailDto : SpecificationSummaryDto
{
    public List<SpecificationWorksheetLinkDto> WorksheetLinks { get; set; } = [];
    public List<SpecificationCharacteristicDto> Characteristics { get; set; } = [];
}

public class SpecificationWorksheetLinkDto : BaseDto
{
    public Guid WorksheetTemplateId { get; set; }

    /// <summary>
    /// The pinned template version this link resolves against. Captured server-side at save
    /// time; a client reads it but never sets it.
    /// </summary>
    public int WorksheetTemplateVersion { get; set; }

    public SpecificationAnalysisType AnalysisType { get; set; }

    /// <summary>Resolved so a client can render the linked template without a second call.</summary>
    public WorksheetTemplateReferenceDto WorksheetTemplate { get; set; }
}

/// <summary>Enough of a worksheet template to render a link to it.</summary>
public class WorksheetTemplateReferenceDto
{
    public Guid Id { get; set; }
    public string Code { get; set; }
    public string Name { get; set; }
    public int Version { get; set; }
    public WorksheetCategory Category { get; set; }
    public QcDocumentStatus Status { get; set; }
}

public class SpecificationCharacteristicDto : BaseDto
{
    public string TestName { get; set; }
    public string Analyte { get; set; }
    public string AcceptanceCriteria { get; set; }
    public string AlertLimit { get; set; }
    public string ActionLimit { get; set; }
    public Guid? SamplingPointGroupId { get; set; }
    public SamplingPointGroupDto SamplingPointGroup { get; set; }
    public Guid SourceWorksheetTemplateId { get; set; }
    public string SourceFieldKey { get; set; }
    public bool IncludeOnCoa { get; set; }
    public int DisplayOrder { get; set; }
    public string GroupName { get; set; }
}

/// <summary>
/// Every enum here is nullable and <c>[Required]</c> on purpose: an omitted enum would
/// otherwise bind to the CLR zero value, which is exactly the silent default the brief
/// forbids for <see cref="RetestPolicy"/> and would quietly pick RawMaterial for
/// <see cref="AppliesTo"/>. Model binding rejects the omission instead.
/// </summary>
public class CreateSpecificationRequest
{
    [Required, StringLength(100)] public string Code { get; set; }
    [Required, StringLength(500)] public string Name { get; set; }
    [Required] public SpecificationAppliesTo? AppliesTo { get; set; }

    /// <summary>Required when <see cref="AppliesTo"/> is Product; must be omitted otherwise.</summary>
    public SpecificationStage? Stage { get; set; }

    [Required] public QcRetestPolicy? RetestPolicy { get; set; }

    public List<CreateSpecificationWorksheetLinkRequest> WorksheetLinks { get; set; } = [];
    public List<CreateSpecificationCharacteristicRequest> Characteristics { get; set; } = [];
}

public class CreateSpecificationWorksheetLinkRequest
{
    [Required] public Guid WorksheetTemplateId { get; set; }
    [Required] public SpecificationAnalysisType? AnalysisType { get; set; }
}

public class CreateSpecificationCharacteristicRequest
{
    [Required, StringLength(200)] public string TestName { get; set; }
    [StringLength(200)] public string Analyte { get; set; }
    [Required, StringLength(2000)] public string AcceptanceCriteria { get; set; }
    [StringLength(500)] public string AlertLimit { get; set; }
    [StringLength(500)] public string ActionLimit { get; set; }

    /// <summary>
    /// An id from the <c>SamplingPointGroup</c> table, or null. There is deliberately no
    /// name/free-text alternative on this request: a typo here would silently apply the
    /// wrong Alert/Action tier.
    /// </summary>
    public Guid? SamplingPointGroupId { get; set; }

    [Required] public Guid SourceWorksheetTemplateId { get; set; }
    [Required, StringLength(100)] public string SourceFieldKey { get; set; }
    public bool IncludeOnCoa { get; set; } = true;
    public int DisplayOrder { get; set; }
    [StringLength(200)] public string GroupName { get; set; }
}

public class UpdateSpecificationRequest : CreateSpecificationRequest;

/// <summary>
/// One selectable source field for a Characteristic, from
/// <c>GET /specifications/{id}/available-fields</c>.
/// <para>
/// Every field on the linked templates is returned rather than only the Result- and
/// CalculatedValue-typed ones: the realistic candidates are those two, but filtering is left
/// to the client so a future field type that warrants COA inclusion needs no backend change.
/// </para>
/// </summary>
public class SpecificationAvailableFieldDto
{
    public Guid WorksheetTemplateId { get; set; }
    public string WorksheetTemplateCode { get; set; }
    public string WorksheetTemplateName { get; set; }

    /// <summary>
    /// The pinned template version these fields were read from — the version the link is
    /// fixed to, never a newer one that has since become Effective.
    /// </summary>
    public int WorksheetTemplateVersion { get; set; }

    public SpecificationAnalysisType AnalysisType { get; set; }
    public string SectionName { get; set; }
    public string FieldKey { get; set; }
    public string Label { get; set; }
    public WorksheetFieldType Type { get; set; }
    public WorksheetFieldMode Mode { get; set; }
    public string Unit { get; set; }
    public string Analyte { get; set; }
}
