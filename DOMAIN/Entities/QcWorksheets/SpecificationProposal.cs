using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.QcWorksheets;

/// <summary>Where a <see cref="SpecificationProposalSet"/> is in its (one-way) review.</summary>
public enum SpecificationProposalStatus
{
    Pending = 0,
    Applied = 1,
    Dismissed = 2
}

/// <summary>
/// The Specification proposals one imported worksheet file carried (build brief 08), kept on
/// the server until a reviewer turns them into a <b>Draft</b> <see cref="Specification"/> or
/// dismisses them.
/// <para>
/// Nothing here is ever created Approved or Effective: applying a set goes through
/// <c>SpecificationRepository</c>'s ordinary create/update, so the M2 lifecycle stays the only
/// path to approval.
/// </para>
/// </summary>
public class SpecificationProposalSet : BaseEntity
{
    /// <summary>ProductMicro, PurifiedWater or EnvironmentalMonitoring; media and certificates produce no set.</summary>
    public ArdFamily Family { get; set; }

    [StringLength(500)] public string SourceFileName { get; set; }

    /// <summary>
    /// The saved worksheet template the characteristics bind to. Required: the import screen
    /// posts a set only after the template it belongs to has been saved.
    /// </summary>
    public Guid WorksheetTemplateId { get; set; }

    public WorksheetTemplate WorksheetTemplate { get; set; }

    /// <summary>From the running header, when printed.</summary>
    [StringLength(500)] public string ProductName { get; set; }

    /// <summary>The printed "Spec. No.", when present.</summary>
    [StringLength(100)] public string SpecificationCode { get; set; }

    /// <summary>
    /// jsonb: <c>specificationProposals</c>, <c>samplingPointGroupProposals</c> and
    /// <c>samplingPointCodes</c>, exactly as the import screen's carry-forward document has
    /// them. Stored whole rather than normalized because it is review input, never queried.
    /// </summary>
    public string ProposalJson { get; set; }

    public SpecificationProposalStatus Status { get; set; } = SpecificationProposalStatus.Pending;

    /// <summary>Set on apply: the Draft Specification this set was created as or appended to.</summary>
    public Guid? AppliedSpecificationId { get; set; }

    public Specification AppliedSpecification { get; set; }

    /// <summary>Required on dismiss, so a discarded proposal always says why.</summary>
    [StringLength(2000)] public string DismissReason { get; set; }
}

// ---------------------------------------------------------------------------
// Requests / DTOs
// ---------------------------------------------------------------------------

/// <summary>The proposal body stored in <see cref="SpecificationProposalSet.ProposalJson"/>.</summary>
public class SpecificationProposalPayload
{
    public List<SpecificationCharacteristicProposal> SpecificationProposals { get; set; } = [];
    public List<SamplingPointGroupProposal> SamplingPointGroupProposals { get; set; } = [];
    public List<string> SamplingPointCodes { get; set; } = [];
}

public class CreateSpecificationProposalSetRequest : SpecificationProposalPayload
{
    [Required] public ArdFamily? Family { get; set; }
    [Required, StringLength(500)] public string SourceFileName { get; set; }
    [Required] public Guid WorksheetTemplateId { get; set; }
    [StringLength(500)] public string ProductName { get; set; }
    [StringLength(100)] public string SpecificationCode { get; set; }
}

public class SpecificationProposalSetSummaryDto : BaseDto
{
    public ArdFamily Family { get; set; }
    public string SourceFileName { get; set; }
    public Guid WorksheetTemplateId { get; set; }
    public WorksheetTemplateReferenceDto WorksheetTemplate { get; set; }
    public string ProductName { get; set; }
    public string SpecificationCode { get; set; }
    public SpecificationProposalStatus Status { get; set; }

    /// <summary>Number of characteristic proposals.</summary>
    public int CharacteristicCount { get; set; }

    /// <summary>Number of sampling point group (limit tier) proposals; 0 for products.</summary>
    public int TierCount { get; set; }

    public Guid? AppliedSpecificationId { get; set; }
    public string DismissReason { get; set; }
}

public class SpecificationProposalSetDetailDto : SpecificationProposalSetSummaryDto
{
    public List<SpecificationCharacteristicProposal> SpecificationProposals { get; set; } = [];
    public List<SamplingPointGroupProposal> SamplingPointGroupProposals { get; set; } = [];
    public List<string> SamplingPointCodes { get; set; } = [];
}

public class SpecificationProposalDraftRequest
{
    [Required] public List<Guid> ProposalSetIds { get; set; } = [];
}

public class SpecificationProposalApplyRequest
{
    [Required] public List<Guid> ProposalSetIds { get; set; } = [];
    [Required] public SpecificationDraftPlan Plan { get; set; }
}

public class SpecificationProposalDismissRequest
{
    [Required, StringLength(2000)] public string Reason { get; set; }
}

public class SpecificationProposalApplyResult
{
    public Guid SpecificationId { get; set; }
}

/// <summary>
/// A reviewable Specification draft built from one or more proposal sets. The same shape as
/// <see cref="CreateSpecificationRequest"/> — with characteristic rows that may name a group
/// not yet created — plus the groups to upsert and the warnings found while building it.
/// Read-only when produced; the reviewer edits it and posts it back to apply.
/// </summary>
public class SpecificationDraftPlan
{
    /// <summary>Set when the plan appends to an existing Draft/UnderReview EM Specification.</summary>
    public Guid? TargetSpecificationId { get; set; }

    public ArdFamily Family { get; set; }

    [StringLength(100)] public string Code { get; set; }
    [StringLength(500)] public string Name { get; set; }
    public SpecificationAppliesTo? AppliesTo { get; set; }
    public SpecificationStage? Stage { get; set; }

    /// <summary>Always null in a produced plan (M2: no default); the reviewer must choose.</summary>
    public QcRetestPolicy? RetestPolicy { get; set; }

    public List<CreateSpecificationWorksheetLinkRequest> WorksheetLinks { get; set; } = [];
    public List<SpecificationDraftPlanCharacteristic> Characteristics { get; set; } = [];
    public List<SpecificationDraftPlanGroup> Groups { get; set; } = [];
    public List<SpecificationDraftPlanWarning> Warnings { get; set; } = [];
}

/// <summary>
/// A <see cref="CreateSpecificationCharacteristicRequest"/> whose tier may be named rather than
/// referenced by id, because on a fresh import the group does not exist until apply creates it.
/// </summary>
public class SpecificationDraftPlanCharacteristic : CreateSpecificationCharacteristicRequest
{
    /// <summary>The limit tier by name; resolved to <see cref="CreateSpecificationCharacteristicRequest.SamplingPointGroupId"/> on apply.</summary>
    [StringLength(200)] public string SamplingPointGroupName { get; set; }
}

public class SpecificationDraftPlanGroup
{
    [StringLength(200)] public string Name { get; set; }
    public string Description { get; set; }

    /// <summary>The live group of this name, when one exists; null means apply creates it.</summary>
    public Guid? SamplingPointGroupId { get; set; }

    public bool IsNew { get; set; }

    /// <summary>The sampling point codes apply assigns to this group.</summary>
    public List<string> PointCodes { get; set; } = [];
}

public class SpecificationDraftPlanWarning
{
    public string Code { get; set; }
    public string Message { get; set; }
}

/// <summary>The warning codes a <see cref="SpecificationDraftPlan"/> can carry.</summary>
public static class SpecificationDraftPlanWarningCodes
{
    /// <summary>Same group name, different limits, across sets. Apply refuses until one row per group remains.</summary>
    public const string TierConflict = "TierConflict";

    /// <summary>Only an Effective EM Specification exists; the plan is for a new one.</summary>
    public const string EffectiveEmSpecificationExists = "EffectiveEmSpecificationExists";

    /// <summary>A proposal's field is not on the pinned template version; the row was left out.</summary>
    public const string FieldNotOnTemplate = "FieldNotOnTemplate";

    /// <summary>The linked template is not Effective. A warning only: M2 lets a Draft link any version.</summary>
    public const string TemplateNotEffective = "TemplateNotEffective";
}
