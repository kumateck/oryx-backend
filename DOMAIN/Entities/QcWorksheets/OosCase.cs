using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.QcWorksheets;

/// <summary>
/// The formal OOS workflow's state machine.
/// <para>
/// <see cref="Open"/> is reached automatically, never by a person — a submitted value breached
/// its Characteristic's Action limit. Everything after it is a deliberate human act.
/// </para>
/// </summary>
public enum OosCaseStatus
{
    Open = 0,
    InvestigationInProgress = 1,
    RetestRequested = 2,
    PendingQaDisposition = 3,
    Closed = 4
}

/// <summary>
/// QA's three-way disposition.
/// <para>
/// Deliberately three-valued rather than the live <c>OosInvestigation</c>'s binary
/// <c>QaApproved</c>/<c>PermanentlyRejected</c>: "the original result was invalid" and "the
/// retest result is the one that counts" are genuinely different findings with different
/// meanings on a certificate, and collapsing them loses which result the COA may draw from.
/// </para>
/// <para>
/// Numbering starts at 1 so the CLR/database zero value means "not yet disposed" rather than
/// silently reading as <see cref="ConfirmedOOS"/> — the same reasoning as
/// <see cref="QcRetestPolicy"/>.
/// </para>
/// </summary>
public enum OosDispositionOutcome
{
    /// <summary>The result stands. The batch is rejected.</summary>
    ConfirmedOOS = 1,

    /// <summary>Assignable lab error found; the original result is void. The batch is released.</summary>
    Invalidated = 2,

    /// <summary>The retest result is the one that counts. The batch is released.</summary>
    RetestAccepted = 3
}

/// <summary>
/// One formal Out-of-Specification investigation, opened automatically when a submitted
/// worksheet value breaches its <see cref="SpecificationCharacteristic"/>'s Action limit.
/// <para>
/// Granularity is per-<see cref="FieldKey"/>, not per-round: one Specification can carry nine
/// or more Characteristics, and a single failing test must not read as "the whole request is
/// OOS." This is the deliberate departure from the live <c>OosInvestigation</c>, which is
/// per-<c>AnalyticalTestRequest</c>.
/// </para>
/// <para>
/// <b>Hard version pinning</b>: this case carries no Specification or WorksheetTemplate
/// reference of its own. Every limit it was judged against, and every template the retest
/// runs, is resolved through <see cref="WorksheetInstanceId"/> — which is already pinned to an
/// exact Specification version (via its round) and an exact WorksheetTemplate version. Nothing
/// here resolves either forward, so a Specification revised after this case opened cannot
/// change what the case means.
/// </para>
/// <para>
/// Approvals are not held as a child collection: the QA disposition signature lives in the
/// shared <see cref="QcApproval"/> table, looked up by (EntityType = "OosCase",
/// EntityId = this Id) — the same centralized table Milestones 1-3 use. There is deliberately
/// no second, OOS-only signature mechanism.
/// </para>
/// </summary>
public class OosCase : BaseEntity, IRequireApproval
{
    /// <summary>
    /// From <see cref="IRequireApproval"/>. Set true only once every required disposition stage
    /// has been approved — which is what gates the real batch status write, so a multi-stage QA
    /// chain cannot release or reject a batch on its first signature.
    /// </summary>
    public bool Approved { get; set; }

    /// <summary>
    /// The worksheet whose submitted value triggered this case. The subject, the round, the
    /// pinned Specification version and the pinned template version all resolve through here,
    /// which is why none of them is stored again.
    /// </summary>
    public Guid WorksheetInstanceId { get; set; }

    public WorksheetInstance WorksheetInstance { get; set; }

    /// <summary>
    /// Which Characteristic-bound Result field failed. The <c>TestRequestSubjectId</c> is
    /// derived via <see cref="WorksheetInstanceId"/> and never stored redundantly — two places
    /// holding the same answer is two places for them to disagree.
    /// </summary>
    [StringLength(100)] public string FieldKey { get; set; }

    public OosCaseStatus Status { get; set; } = OosCaseStatus.Open;

    /// <summary>
    /// System-set at auto-creation. There is deliberately no "OpenedById": this case was not
    /// opened by a person, it was opened by a limit breach, and naming an analyst here would
    /// misattribute an automatic act to them.
    /// </summary>
    public DateTime OpenedAt { get; set; }

    /// <summary>
    /// The value that breached, captured at detection. Kept on the case rather than re-read
    /// from the worksheet at display time: the case must still read correctly beside a retest,
    /// and the record of what triggered it is the point.
    /// </summary>
    [StringLength(2000)] public string ObservedValue { get; set; }

    /// <summary>The Action limit text (or acceptance criteria, when no Action limit exists) the value was judged against.</summary>
    [StringLength(2000)] public string BreachedLimit { get; set; }

    /// <summary>
    /// The Characteristic whose limit was breached, for the detail view's "submitted value vs.
    /// the limit that triggered it". Restrict-deleted, so the row stays readable.
    /// </summary>
    public Guid? SpecificationCharacteristicId { get; set; }

    public SpecificationCharacteristic SpecificationCharacteristic { get; set; }

    // --- Phase 1 investigation (lab error check) ---------------------------

    public string InvestigationDetails { get; set; }

    public string RootCauseAnalysis { get; set; }

    public string CorrectiveActions { get; set; }

    public string PreventiveActions { get; set; }

    public Guid? InvestigatedById { get; set; }

    public User InvestigatedBy { get; set; }

    public DateTime? InvestigatedAt { get; set; }

    // --- Retest ------------------------------------------------------------

    public Guid? RetestAuthorizedById { get; set; }

    public User RetestAuthorizedBy { get; set; }

    public DateTime? RetestAuthorizedAt { get; set; }

    /// <summary>
    /// The new instance created for the retest. This is the far side of Milestone 3's
    /// <see cref="WorksheetInstance.RetestOfInstanceId"/>, which points back at the original.
    /// <para>
    /// A retest is always a <b>new</b> instance, never the original reopened: the original's
    /// FieldValues are never touched, and both results stay visible in the trace. This is the
    /// deliberate behaviour change from the live system's reopen-in-place pattern.
    /// </para>
    /// </summary>
    public Guid? RetestWorksheetInstanceId { get; set; }

    public WorksheetInstance RetestWorksheetInstance { get; set; }

    // --- QA disposition ----------------------------------------------------

    /// <summary>
    /// Recorded when the disposition is signed for, but only acted on once
    /// <see cref="Approved"/> becomes true.
    /// </summary>
    public OosDispositionOutcome? DispositionOutcome { get; set; }

    public Guid? DispositionById { get; set; }

    public User DispositionBy { get; set; }

    public DateTime? DispositionAt { get; set; }

    public string DispositionComments { get; set; }

    // --- Real batch quarantine --------------------------------------------

    /// <summary>
    /// Set when the investigation starts — not at auto-creation — and cleared at close. This is
    /// the one place in the rebuilt QC module that writes to a pre-existing live entity: a
    /// QC-only shadow quarantine that Warehouse and Production could not see would defeat the
    /// point of quarantining a batch.
    /// </summary>
    public Guid? QuarantinedMaterialBatchId { get; set; }

    public MaterialBatch QuarantinedMaterialBatch { get; set; }

    public Guid? QuarantinedBatchManufacturingRecordId { get; set; }

    public BatchManufacturingRecord QuarantinedBatchManufacturingRecord { get; set; }

    /// <summary>
    /// The batch's own status immediately before this case quarantined it, captured purely as
    /// an audit record on <b>this</b> table — no schema change to <c>MaterialBatches</c>.
    /// <para>
    /// "What state was this batch in before QC locked it out?" is a real question during a
    /// regulatory review, and once the status is overwritten nothing else in the system retains
    /// the answer. It is deliberately <i>not</i> read back by the disposition: closing a case
    /// writes the outcome's own status, it does not rewind.
    /// </para>
    /// </summary>
    public BatchStatus? QuarantinedFromBatchStatus { get; set; }

    /// <summary>The same audit capture for a Product round. See <see cref="QuarantinedFromBatchStatus"/>.</summary>
    public BatchManufacturingStatus? QuarantinedFromBatchManufacturingStatus { get; set; }
}
