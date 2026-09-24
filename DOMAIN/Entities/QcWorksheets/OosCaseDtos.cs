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

/// <summary>
/// An OOS case as seen from the worksheet it was opened against, carried on
/// <see cref="WorksheetInstanceDetailDto"/>.
/// <para>
/// Deliberately narrower than <see cref="OosCaseSummaryDto"/>: every field here comes off the
/// <see cref="OosCase"/> row itself, so attaching it costs one indexed read and no joins. The
/// summary DTO's subject, round and template details would all be duplicates of what the
/// worksheet's own header already carries.
/// </para>
/// <para>
/// This exists so a reviewer can ask "is this worksheet under an OOS case, and what came of
/// it" in the call that already loads the worksheet. The alternative — re-deriving the answer
/// by parsing Specification acceptance-criteria text client-side — can disagree with the
/// backend's tested <c>LimitEvaluator</c> grammar, and a reviewer disagreeing with the system
/// of record about whether a result is out of specification is the failure worth designing
/// out.
/// </para>
/// </summary>
public class WorksheetInstanceOosCaseDto
{
    public Guid Id { get; set; }

    /// <summary>Which Result field breached. One worksheet can hold several cases, one per field.</summary>
    public string FieldKey { get; set; }

    public OosCaseStatus Status { get; set; }

    public DateTime OpenedAt { get; set; }

    /// <summary>The value that breached and the limit it breached, as captured at detection.</summary>
    public string ObservedValue { get; set; }

    public string BreachedLimit { get; set; }

    /// <summary>Null until QA has signed a disposition; set to the three-way outcome after.</summary>
    public OosDispositionOutcome? DispositionOutcome { get; set; }

    /// <summary>True while this case is unclosed, and therefore still holding its round.</summary>
    public bool BlocksRelease { get; set; }

    /// <summary>The linked retest worksheet, once one has been authorized.</summary>
    public Guid? RetestWorksheetInstanceId { get; set; }
}

/// <summary>
/// An OOS case as seen from the round it is holding, carried on
/// <see cref="TestRequestDetailDto"/>.
/// <para>
/// A sibling of <see cref="WorksheetInstanceOosCaseDto"/> rather than a reuse of it, for two
/// reasons. It needs <see cref="WorksheetInstanceId"/>, which that DTO deliberately omits
/// because its parent already is the worksheet — at round level the worksheet is exactly the
/// thing the UI has to link back to. And it drops <c>BlocksRelease</c>, which would be
/// uniformly true here: membership of this list <i>is</i> the blocking state, so carrying a
/// per-row flag would invite a reader to filter on a column that is never false.
/// </para>
/// <para>
/// Everything projected comes off the <see cref="OosCase"/> row itself, so attaching the list
/// costs one indexed read and no joins.
/// </para>
/// </summary>
public class TestRequestBlockingOosCaseDto
{
    public Guid Id { get; set; }

    /// <summary>
    /// The worksheet the case was opened against. The round detail already lists every
    /// worksheet under every subject, so this is enough for the UI to locate the subject row
    /// without a second lookup.
    /// </summary>
    public Guid WorksheetInstanceId { get; set; }

    /// <summary>Which Result field breached. One worksheet can hold several cases, one per field.</summary>
    public string FieldKey { get; set; }

    /// <summary>
    /// Never <see cref="OosCaseStatus.Closed"/> — a closed case does not block and is not
    /// listed. Carried so the UI can say <i>why</i> the round is held: awaiting investigation
    /// reads differently from awaiting a QA signature.
    /// </summary>
    public OosCaseStatus Status { get; set; }

    /// <summary>Detection time. The list is ordered by this, oldest first.</summary>
    public DateTime OpenedAt { get; set; }
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
