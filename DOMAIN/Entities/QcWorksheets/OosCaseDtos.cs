using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.QcWorksheets;

// ---------------------------------------------------------------------------
// OosCase — reads
// ---------------------------------------------------------------------------

/// <summary>The list row: what failed, on which subject, and how long it has been open.</summary>
public class OosCaseSummaryDto : BaseDto
{
    public Guid WorksheetInstanceId { get; set; }
    public string FieldKey { get; set; }
    public OosCaseStatus Status { get; set; }
    public DateTime OpenedAt { get; set; }

    /// <summary>The value that breached and the limit it breached, so the list reads without opening each case.</summary>
    public string ObservedValue { get; set; }

    public string BreachedLimit { get; set; }

    /// <summary>The failing test's name, from the Characteristic — more useful in a list than a raw FieldKey.</summary>
    public string TestName { get; set; }

    // Derived through the worksheet instance, never stored on the case itself.
    public Guid TestRequestSubjectId { get; set; }
    public string SubjectRef { get; set; }
    public string SubjectLabel { get; set; }
    public Guid TestRequestId { get; set; }
    public string ArNumber { get; set; }
    public string WorksheetTemplateCode { get; set; }

    public bool Approved { get; set; }
    public OosDispositionOutcome? DispositionOutcome { get; set; }
    public Guid? RetestWorksheetInstanceId { get; set; }
}

/// <summary>
/// The full case: the breach in context, the Phase 1 investigation, the retest link and the
/// disposition.
/// </summary>
public class OosCaseDetailDto : OosCaseSummaryDto
{
    /// <summary>
    /// The pinned Specification version the limit came from — the version the round runs
    /// against, never whichever version is Effective at read time.
    /// </summary>
    public Guid SpecificationId { get; set; }

    public int SpecificationVersion { get; set; }
    public string SpecificationCode { get; set; }
    public Guid? SpecificationCharacteristicId { get; set; }
    public string AcceptanceCriteria { get; set; }
    public string AlertLimit { get; set; }
    public string ActionLimit { get; set; }

    /// <summary>Drives whether a retest reuses this sample or demands a fresh one. Read-only here; set on the Specification.</summary>
    public QcRetestPolicy RetestPolicy { get; set; }

    public string InvestigationDetails { get; set; }
    public string RootCauseAnalysis { get; set; }
    public string CorrectiveActions { get; set; }
    public string PreventiveActions { get; set; }
    public UserDto InvestigatedBy { get; set; }
    public DateTime? InvestigatedAt { get; set; }

    public UserDto RetestAuthorizedBy { get; set; }
    public DateTime? RetestAuthorizedAt { get; set; }

    /// <summary>
    /// The original result and the retest result side by side. The original is never replaced
    /// in this view — both stay visible, which is the entire point of a linked retest.
    /// </summary>
    public WorksheetInstanceSummaryDto WorksheetInstance { get; set; }

    public WorksheetInstanceSummaryDto RetestWorksheetInstance { get; set; }

    public UserDto DispositionBy { get; set; }
    public DateTime? DispositionAt { get; set; }
    public string DispositionComments { get; set; }

    public Guid? QuarantinedMaterialBatchId { get; set; }
    public Guid? QuarantinedBatchManufacturingRecordId { get; set; }

    /// <summary>
    /// True while this case is stopping its round from being released. Exposed so the round and
    /// certificate screens can say <i>why</i> a release is unavailable rather than just
    /// disabling a button.
    /// </summary>
    public bool BlocksRelease { get; set; }
}

// ---------------------------------------------------------------------------
// OosCase — writes
// ---------------------------------------------------------------------------

/// <summary>Phase 1: the lab error check. Editable only while InvestigationInProgress.</summary>
public class UpdateOosInvestigationRequest
{
    public string InvestigationDetails { get; set; }
    public string RootCauseAnalysis { get; set; }
    public string CorrectiveActions { get; set; }
    public string PreventiveActions { get; set; }
}

/// <summary>
/// Authorizes a retest. Which sample it runs against is decided by the Specification's
/// <see cref="QcRetestPolicy"/>, not by the caller — the policy exists precisely so this is not
/// a per-case judgement call.
/// </summary>
public class AuthorizeOosRetestRequest
{
    /// <summary>
    /// The fresh sample's collection time. Used only when the Specification's policy is
    /// <see cref="QcRetestPolicy.FreshResample"/>, where a new Subject is created and must
    /// carry its own CollectedAt; defaults to now. Ignored for SameSample, which reuses the
    /// original Subject and its original collection time.
    /// </summary>
    public DateTime? CollectedAt { get; set; }

    /// <summary>The fresh sample's own sub-number, when the lab assigns one. FreshResample only.</summary>
    [StringLength(100)] public string ArNumber { get; set; }

    /// <summary>Why a retest is justified — the Phase 1 finding that supports it.</summary>
    [StringLength(1000)] public string Reason { get; set; }
}

/// <summary>Escalates straight to QA without a retest: no lab error was found.</summary>
public class EscalateOosCaseRequest
{
    [StringLength(1000)] public string Reason { get; set; }
}

/// <summary>
/// The QA disposition. Carries the re-authentication credential because a disposition is a
/// meaning-of-signature event — a valid session alone is deliberately not enough.
/// </summary>
public class OosDispositionRequest
{
    public OosDispositionOutcome? Outcome { get; set; }

    /// <summary>The acting user's own password, re-entered. Verified against their credentials, never stored.</summary>
    public string Password { get; set; }

    public string DispositionComments { get; set; }
}
