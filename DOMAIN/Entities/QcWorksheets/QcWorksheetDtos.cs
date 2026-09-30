using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.QcWorksheets;

// ---------------------------------------------------------------------------
// StandardTestProcedure
// ---------------------------------------------------------------------------

public class StpSummaryDto : BaseDto
{
    public string Code { get; set; }
    public string Name { get; set; }
    public string Area { get; set; }
    public int Version { get; set; }
    public QcDocumentStatus Status { get; set; }
    public bool Approved { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public DateTime? ReviewDate { get; set; }
    public DateTime? IssueDate { get; set; }
    public Guid? SupersedesId { get; set; }
}

public class StpDetailDto : StpSummaryDto
{
    public string Purpose { get; set; }
    public string Scope { get; set; }
    public string Responsibility { get; set; }
    public string Accountability { get; set; }
    public List<StpStepDto> Steps { get; set; } = [];
}

public class StpStepDto : BaseDto
{
    public int Order { get; set; }
    public string Title { get; set; }
    public string Instruction { get; set; }

    /// <summary>
    /// The resolved cross-referenced STP, so a client can render a real link rather than
    /// plain text. Null when the step references nothing.
    /// </summary>
    public StpReferenceDto ReferencedStp { get; set; }
}

/// <summary>Enough of a referenced STP to render a link to it.</summary>
public class StpReferenceDto
{
    public Guid Id { get; set; }
    public string Code { get; set; }
    public string Name { get; set; }
    public int Version { get; set; }
    public QcDocumentStatus Status { get; set; }
}

public class CreateStpRequest
{
    [Required, StringLength(100)] public string Code { get; set; }
    [Required, StringLength(500)] public string Name { get; set; }
    [StringLength(200)] public string Area { get; set; }
    public string Purpose { get; set; }
    public string Scope { get; set; }
    public string Responsibility { get; set; }
    public string Accountability { get; set; }
    public DateTime? ReviewDate { get; set; }
    public DateTime? IssueDate { get; set; }
    public List<CreateStpStepRequest> Steps { get; set; } = [];
}

public class CreateStpStepRequest
{
    public int Order { get; set; }
    [StringLength(200)] public string Title { get; set; }
    [Required] public string Instruction { get; set; }
    public Guid? ReferencedStpId { get; set; }
}

/// <summary>Same shape as create, per the brief.</summary>
public class UpdateStpRequest : CreateStpRequest;

// ---------------------------------------------------------------------------
// WorksheetTemplate
// ---------------------------------------------------------------------------

public class WorksheetTemplateSummaryDto : BaseDto
{
    public string Code { get; set; }
    public string Name { get; set; }
    public string Department { get; set; }
    public WorksheetCategory Category { get; set; }
    public int Version { get; set; }
    public QcDocumentStatus Status { get; set; }
    public bool Approved { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public Guid? SupersedesId { get; set; }
    public Guid? StpId { get; set; }
}

public class WorksheetTemplateDetailDto : WorksheetTemplateSummaryDto
{
    public StpReferenceDto Stp { get; set; }
    public List<WorksheetSectionDto> Sections { get; set; } = [];
}

public class WorksheetSectionDto : BaseDto
{
    public int Order { get; set; }
    public string Name { get; set; }
    public Guid? InstrumentId { get; set; }
    public List<WorksheetFieldDto> Fields { get; set; } = [];
}

public class WorksheetFieldDto : BaseDto
{
    public int Order { get; set; }
    public string FieldKey { get; set; }
    public string Label { get; set; }
    public WorksheetFieldType Type { get; set; }
    public WorksheetFieldMode Mode { get; set; }
    public string Unit { get; set; }
    public string Analyte { get; set; }
    public string ConstantValue { get; set; }
    public string FormulaExpression { get; set; }
    public string ColumnDefinitions { get; set; }

    /// <summary>JSON string array; null when the field carries no options.</summary>
    public string OptionsJson { get; set; }

    public Guid? ReferencedResultSourceTemplateId { get; set; }
    public string ReferencedResultSourceFieldKey { get; set; }
    public string ReferencedResultResolutionFieldKey { get; set; }
    public List<WorksheetFieldRevisionDto> Revisions { get; set; } = [];
}

public class WorksheetFieldRevisionDto : BaseDto
{
    public int RevisionNumber { get; set; }
    public string FieldKey { get; set; }
    public string Label { get; set; }
    public WorksheetFieldType Type { get; set; }
    public WorksheetFieldMode Mode { get; set; }
    public string Unit { get; set; }
    public string Analyte { get; set; }
    public string ConstantValue { get; set; }
    public string FormulaExpression { get; set; }
    public string ColumnDefinitions { get; set; }
    public string OptionsJson { get; set; }
    public int Order { get; set; }
}

public class CreateWorksheetTemplateRequest
{
    [Required, StringLength(100)] public string Code { get; set; }
    [Required, StringLength(500)] public string Name { get; set; }
    [StringLength(200)] public string Department { get; set; }
    public Guid? StpId { get; set; }
    public WorksheetCategory Category { get; set; }
    public List<CreateWorksheetSectionRequest> Sections { get; set; } = [];
}

public class CreateWorksheetSectionRequest
{
    public int Order { get; set; }
    [Required, StringLength(200)] public string Name { get; set; }
    public Guid? InstrumentId { get; set; }
    public List<CreateWorksheetFieldRequest> Fields { get; set; } = [];
}

public class CreateWorksheetFieldRequest
{
    public int Order { get; set; }
    [Required, StringLength(100)] public string FieldKey { get; set; }
    [Required, StringLength(500)] public string Label { get; set; }
    public WorksheetFieldType Type { get; set; }
    public WorksheetFieldMode Mode { get; set; }
    [StringLength(50)] public string Unit { get; set; }
    [StringLength(200)] public string Analyte { get; set; }
    public string ConstantValue { get; set; }
    public string FormulaExpression { get; set; }
    public string ColumnDefinitions { get; set; }

    /// <summary>
    /// Required (at least two distinct, non-blank strings) for Select, MultiSelect and
    /// GrowthObservation; refused on every other type. Stored trimmed and de-duplicated.
    /// </summary>
    public string OptionsJson { get; set; }

    public Guid? ReferencedResultSourceTemplateId { get; set; }
    [StringLength(100)] public string ReferencedResultSourceFieldKey { get; set; }
    [StringLength(100)] public string ReferencedResultResolutionFieldKey { get; set; }
}

public class UpdateWorksheetTemplateRequest : CreateWorksheetTemplateRequest;

// ---------------------------------------------------------------------------
// Approval / e-signature
// ---------------------------------------------------------------------------

/// <summary>
/// The QC re-authentication envelope. <see cref="Password"/> is verified against the
/// <b>current</b> user's own credentials — this is the acting user re-proving themselves,
/// which is what meaning-of-signature requires; it is not a generic password check.
/// </summary>
public class QcApprovalRequest
{
    [Required] public string Password { get; set; }

    /// <summary>
    /// Carries what an earlier design called "Meaning"/"ReasonForChange". Stored on the
    /// base class's <c>Comments</c> column.
    /// </summary>
    [StringLength(1000)] public string Comments { get; set; }
}

/// <summary>Manual supersession outside the normal new-version flow. Reason is mandatory.</summary>
public class QcSupersedeRequest
{
    [Required] public string Password { get; set; }
    [Required, StringLength(1000)] public string Comments { get; set; }
}

public class QcApprovalDto
{
    public Guid Id { get; set; }
    public string EntityType { get; set; }
    public Guid EntityId { get; set; }
    public Guid ApprovalId { get; set; }
    public int ApprovalRound { get; set; }
    public int Order { get; set; }
    public bool Required { get; set; }
    public DOMAIN.Entities.Approvals.ApprovalStatus Status { get; set; }
    public DateTime? ApprovalTime { get; set; }
    public DateTime? ReauthConfirmedAt { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public string Comments { get; set; }
    public UserDto ApprovedBy { get; set; }
    public UserDto User { get; set; }
    public Guid? RoleId { get; set; }
}

/// <summary>One row of the centralized QC approvals queue.</summary>
public class QcPendingApprovalDto
{
    public string EntityType { get; set; }
    public Guid EntityId { get; set; }
    public string Code { get; set; }
    public string Name { get; set; }
    public int Version { get; set; }
    public QcDocumentStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public UserDto CreatedBy { get; set; }
    public int Order { get; set; }
    public int ApprovalRound { get; set; }

    /// <summary>The route segment a client posts the re-auth approve/reject to.</summary>
    public string ResourcePath { get; set; }
}

// ---------------------------------------------------------------------------
// DOCX import
// ---------------------------------------------------------------------------

public class StpImportResultDto
{
    public string FileName { get; set; }
    public bool Succeeded { get; set; }

    /// <summary>The created Draft's Id when <see cref="Succeeded"/>.</summary>
    public Guid? StandardTestProcedureId { get; set; }

    public string Code { get; set; }
    public string Name { get; set; }

    /// <summary>Why the file could not be parsed, when <see cref="Succeeded"/> is false.</summary>
    public string FailureReason { get; set; }

    /// <summary>
    /// Fields parsed with low confidence that a human must confirm before the draft is
    /// submitted. An import is still a success when this is non-empty.
    /// </summary>
    public List<string> FlaggedForReview { get; set; } = [];
}
