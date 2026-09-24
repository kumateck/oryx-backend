using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.QcWorksheets;

/// <summary>
/// What a <see cref="TestRequest"/> round tests. Mirrors <see cref="SpecificationAppliesTo"/>
/// value for value — a round tests against exactly one Specification, and the two must agree.
/// <para>
/// Deliberately a separate enum rather than reusing <see cref="SpecificationAppliesTo"/>: the
/// two are free to diverge later (a Specification could gain a category that is never itself
/// a testable round), and <see cref="QcTestRequestTypes"/> holds the one mapping between them
/// so the agreement is enforced in a single place rather than assumed by a shared type.
/// </para>
/// </summary>
public enum TestRequestType
{
    RawMaterial = 0,
    PackagingMaterial = 1,
    Product = 2,
    RoutineWater = 3,
    RoutineEnvironmental = 4
}

/// <summary>
/// Whether the round came from a schedule (Milestone 6's <c>MonitoringProgram</c>, or a manual
/// override of one) or was raised ad hoc. An unscheduled round must carry a reason.
/// </summary>
public enum TestRequestScheduleOrigin
{
    Scheduled = 0,
    Unscheduled = 1
}

/// <summary>
/// The round's lifecycle.
/// <para>
/// Everything from <see cref="Assigned"/> through <see cref="UnderReview"/> is <b>derived</b>
/// from the round's own WorksheetInstances rather than driven by its own endpoints — see
/// <c>TestRequestRepository.RecalculateStatus</c>. <see cref="Released"/> and
/// <see cref="Rejected"/> are defined here but never written by this milestone: releasing a
/// round depends on OOS disposition (Milestone 4) and certificate issue (Milestone 5).
/// </para>
/// </summary>
public enum TestRequestStatus
{
    Draft = 0,
    Sampled = 1,
    Assigned = 2,
    InTesting = 3,
    ResultsComplete = 4,
    UnderReview = 5,
    Released = 6,
    Rejected = 7
}

/// <summary>
/// One worksheet's execution lifecycle.
/// <para>
/// <see cref="Locked"/> is defined but never written by this milestone — a worksheet locks
/// when the certificate drawing on it is issued (Milestone 5). It is honoured as a terminal
/// state everywhere it is read: no reassignment, no edit, no review.
/// </para>
/// </summary>
public enum WorksheetInstanceStatus
{
    NotStarted = 0,
    InProgress = 1,
    Submitted = 2,
    Reviewed = 3,
    Locked = 4
}

/// <summary>
/// Maps a round's <see cref="TestRequestType"/> onto the <see cref="SpecificationAppliesTo"/>
/// its Specification must declare. One round tests against one Specification, so the two must
/// agree; keeping the mapping here means the rule is stated once.
/// </summary>
public static class QcTestRequestTypes
{
    public static SpecificationAppliesTo ToAppliesTo(TestRequestType type) => type switch
    {
        TestRequestType.RawMaterial => SpecificationAppliesTo.RawMaterial,
        TestRequestType.PackagingMaterial => SpecificationAppliesTo.PackagingMaterial,
        TestRequestType.Product => SpecificationAppliesTo.Product,
        TestRequestType.RoutineWater => SpecificationAppliesTo.RoutineWater,
        TestRequestType.RoutineEnvironmental => SpecificationAppliesTo.RoutineEnvironmental,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    /// <summary>A batch-bearing round: its Subjects name material batches.</summary>
    public static bool IsMaterial(TestRequestType type) =>
        type is TestRequestType.RawMaterial or TestRequestType.PackagingMaterial;

    /// <summary>A sampling-point round: its Subjects name points, not batches.</summary>
    public static bool IsRoutine(TestRequestType type) =>
        type is TestRequestType.RoutineWater or TestRequestType.RoutineEnvironmental;
}

/// <summary>
/// The Analytical Request Document — one testing round.
/// <para>
/// A round covers <b>many</b> subjects: a real Environmental Monitoring round runs ~90 rooms
/// and a Water round 15+ sampling points, which is why the subject is an explicit
/// <see cref="TestRequestSubject"/> child rather than a scalar field on this entity. A
/// Material/Product round normally carries exactly one Subject; the shape is unified rather
/// than forked per category.
/// </para>
/// <para>
/// Additive: this is a new table. It neither reads nor writes the live
/// <c>AnalyticalTestRequest</c> path, which keeps running unchanged alongside it.
/// </para>
/// </summary>
public class TestRequest : BaseEntity
{
    public TestRequestType Type { get; set; }

    /// <summary>
    /// The exact Specification version row this round is pinned to. Each version is its own
    /// row, so this id already names one version.
    /// </summary>
    public Guid SpecificationId { get; set; }

    public Specification Specification { get; set; }

    /// <summary>
    /// The pinned version number, captured server-side from the Specification row at creation.
    /// Never client-supplied.
    /// <para>
    /// <b>Hard version pinning</b> (lifecycle-and-governance.md, "Version pinning"): this
    /// round stays on the Specification version that was in force when it was created, for
    /// good. No in-flight upgrade, not even for a non-breaking revision — the ARD alone must
    /// reconstruct what ran, with no change log to cross-reference. If the Specification moves
    /// to v4 while this round is in flight, this round stays on v3 forever, including after v4
    /// becomes Effective. Nothing in this module resolves a Specification forward.
    /// </para>
    /// </summary>
    public int SpecificationVersion { get; set; }

    public TestRequestScheduleOrigin ScheduleOrigin { get; set; }

    /// <summary>
    /// Mandatory when <see cref="ScheduleOrigin"/> is Unscheduled, enforced in
    /// <c>TestRequestRepository</c> rather than by a database constraint — a conditional
    /// NOT NULL is not expressible here, and the rule belongs with the rest of the round's
    /// validation.
    /// </summary>
    [StringLength(1000)] public string UnscheduledReason { get; set; }

    /// <summary>Round-level AR number. A Subject may carry its own sub-number beneath it.</summary>
    [StringLength(100)] public string ArNumber { get; set; }

    [StringLength(100)] public string IssueNumber { get; set; }

    public DateTime? IssuedAt { get; set; }

    public Guid? IssuedById { get; set; }

    public User IssuedBy { get; set; }

    public TestRequestStatus Status { get; set; } = TestRequestStatus.Draft;

    public List<TestRequestSubject> Subjects { get; set; } = [];
}

/// <summary>
/// One subject of a round: a batch (Material/Product — normally exactly one per round) or a
/// sampling point (Water/EM — normally many per round).
/// </summary>
public class TestRequestSubject : BaseEntity
{
    public Guid TestRequestId { get; set; }

    public TestRequest TestRequest { get; set; }

    /// <summary>The batch number, or the sampling point code (e.g. "SF-91").</summary>
    [StringLength(200)] public string SubjectRef { get; set; }

    /// <summary>The human name printed alongside the code on real EM sheets, e.g. "Deblistering-2".</summary>
    [StringLength(500)] public string SubjectLabel { get; set; }

    /// <summary>
    /// The per-point AR sub-number, nested under the round's own <see cref="TestRequest.ArNumber"/>
    /// — confirmed on the real Water worksheet, where each point gets its own number.
    /// </summary>
    [StringLength(100)] public string ArNumber { get; set; }

    /// <summary>
    /// Water/EM only. Drives which Alert/Action tier a submitted result is judged against
    /// (Milestone 4), which is why it is chosen from the group table and never typed.
    /// </summary>
    public Guid? SamplingPointGroupId { get; set; }

    public SamplingPointGroup SamplingPointGroup { get; set; }

    /// <summary>
    /// Water/EM only, and deliberately without a navigation property or foreign key yet: the
    /// <c>SamplingPoint</c> table it will reference does not exist until Milestone 6
    /// (build-briefs/06-monitoring-programs-and-water-quality.md), which introduces the entity
    /// and adds the constraint. The column exists now because the brief lists it on this
    /// entity, and it is auto-populated only by a MonitoringProgram; no endpoint in this
    /// milestone accepts it, so it cannot be set to an unresolvable value in the meantime.
    /// </summary>
    public Guid? SamplingPointId { get; set; }

    /// <summary>
    /// RawMaterial/PackagingMaterial only. Milestone 4's OOS quarantine needs a real batch to
    /// act on rather than a <see cref="SubjectRef"/> string, and the header block's Mfg/Exp
    /// dates resolve through it.
    /// </summary>
    public Guid? MaterialBatchId { get; set; }

    public MaterialBatch MaterialBatch { get; set; }

    /// <summary>Product only — the same reasoning as <see cref="MaterialBatchId"/>.</summary>
    public Guid? BatchManufacturingRecordId { get; set; }

    public BatchManufacturingRecord BatchManufacturingRecord { get; set; }

    public DateTime? CollectedAt { get; set; }

    public List<WorksheetInstance> WorksheetInstances { get; set; } = [];
}

/// <summary>
/// One worksheet being executed, for one Subject. Materialized at round creation, one per
/// (Subject × the pinned Specification's WorksheetLink).
/// <para>
/// Approvals are not held as a child collection: they live in the shared
/// <see cref="QcApproval"/> table, looked up by (EntityType = "WorksheetInstance",
/// EntityId = this Id) — the same centralized table Milestones 1 and 2 use.
/// </para>
/// </summary>
public class WorksheetInstance : BaseEntity, IRequireApproval
{
    /// <summary>From <see cref="IRequireApproval"/>; set true once every required review stage is approved.</summary>
    public bool Approved { get; set; }

    public Guid TestRequestSubjectId { get; set; }

    public TestRequestSubject TestRequestSubject { get; set; }

    /// <summary>The exact worksheet template version row this instance renders and records against.</summary>
    public Guid WorksheetTemplateId { get; set; }

    public WorksheetTemplate WorksheetTemplate { get; set; }

    /// <summary>
    /// The pinned template version, copied at creation from the
    /// <see cref="SpecificationWorksheetLink.WorksheetTemplateVersion"/> the instance was
    /// materialized from — not re-read from whichever template version is Effective now.
    /// <para>
    /// <b>Hard version pinning</b>, the same locked rule as
    /// <see cref="TestRequest.SpecificationVersion"/>: an in-flight worksheet never upgrades,
    /// even for a non-breaking template revision. Every read path in this module resolves this
    /// instance's template by <see cref="WorksheetTemplateId"/> alone, and nothing walks
    /// <c>SupersedesId</c> forward.
    /// </para>
    /// </summary>
    public int WorksheetTemplateVersion { get; set; }

    /// <summary>
    /// Which analysis track this instance belongs to, carried over from the WorksheetLink that
    /// produced it. Reuses <see cref="SpecificationAnalysisType"/> rather than declaring a
    /// second identical enum, so the assignment from the link is type-safe and the Test Room's
    /// Chemical/Microbial split cannot drift from the Specification's.
    /// </summary>
    public SpecificationAnalysisType AnalysisType { get; set; }

    /// <summary>
    /// The one user who may start, enter values into, or submit this instance — enforced
    /// server-side, not merely hidden in the Test Room UI. This is what makes an entry
    /// attributable in the GxP sense.
    /// </summary>
    public Guid? AssignedToId { get; set; }

    public User AssignedTo { get; set; }

    public Guid? AssignedById { get; set; }

    public User AssignedBy { get; set; }

    public DateTime? AssignedAt { get; set; }

    /// <summary>
    /// Set only by Milestone 4's OOS retest flow. This milestone defines the column and never
    /// writes to it: a retest is a new instance linked back here, never the original reopened.
    /// </summary>
    public Guid? RetestOfInstanceId { get; set; }

    public WorksheetInstance RetestOfInstance { get; set; }

    public WorksheetInstanceStatus Status { get; set; } = WorksheetInstanceStatus.NotStarted;

    /// <summary>
    /// When the analyst submitted. Beyond the brief's property list by one field, and
    /// deliberately: the header block the brief specifies prints an Analysis Date ending at
    /// "Submitted timestamp", and no other column records it —<c>UpdatedAt</c> moves again on
    /// every later write, so it cannot stand in for this.
    /// </summary>
    public DateTime? SubmittedAt { get; set; }

    public List<WorksheetFieldValue> FieldValues { get; set; } = [];

    public List<WorksheetInstanceReassignment> Reassignments { get; set; } = [];

    /// <summary>
    /// Every time this worksheet was sent back, oldest first. A worksheet can go round the
    /// correction loop more than once, and each cycle is its own row.
    /// </summary>
    public List<WorksheetInstanceCorrectionReturn> CorrectionReturns { get; set; } = [];
}

/// <summary>
/// One entered value. A scalar field is one row; a Table cell is one row per
/// (RowIndex, ColumnKey); a Reagent/ReferenceStandard entry is three rows sharing a FieldKey
/// and differentiated by <see cref="ColumnKey"/> — see <see cref="QcWorksheetValueColumns"/>.
/// </summary>
public class WorksheetFieldValue : BaseEntity
{
    public Guid WorksheetInstanceId { get; set; }

    public WorksheetInstance WorksheetInstance { get; set; }

    /// <summary>The <c>WorksheetField.FieldKey</c> this value belongs to.</summary>
    [StringLength(100)] public string FieldKey { get; set; }

    /// <summary>Null for a scalar field; the 0-based row number when the field is a Table.</summary>
    public int? RowIndex { get; set; }

    /// <summary>
    /// Null for a plain scalar value. Set for a Table cell (matching a key in the field's
    /// ColumnDefinitions), and for the composite Reagent/ReferenceStandard shape
    /// (<see cref="QcWorksheetValueColumns.ReagentId"/> / <c>BatchNo</c> / <c>ExpiryDate</c>).
    /// </summary>
    [StringLength(100)] public string ColumnKey { get; set; }

    public string Value { get; set; }

    /// <summary>
    /// Who actually typed this value. Never rewritten by a reassignment: attribution follows
    /// the person who was permitted to be doing the work at that moment, which is the whole
    /// point of assignment enforcement.
    /// </summary>
    public Guid EnteredById { get; set; }

    public User EnteredBy { get; set; }

    public DateTime EnteredAt { get; set; }

    /// <summary>
    /// Set only for a ReferencedResult field: the source instance the value was resolved from,
    /// captured at submit so the trace survives even if the source is later superseded.
    /// </summary>
    public Guid? ResolvedFromInstanceId { get; set; }

    public WorksheetInstance ResolvedFromInstance { get; set; }
}

/// <summary>
/// A plain audit record of a worksheet changing hands.
/// <para>
/// Deliberately <b>not</b> routed through the Approval engine and deliberately not a
/// <see cref="QcApproval"/> row: reassigning who performs a test is an administrative act, not
/// a meaning-of-signature event. It records who moved the work, from whom, to whom, when and
/// why; it never rewrites <c>EnteredBy</c> on values already entered, and never resets the
/// instance's Status.
/// </para>
/// </summary>
public class WorksheetInstanceReassignment : BaseEntity
{
    public Guid WorksheetInstanceId { get; set; }

    public WorksheetInstance WorksheetInstance { get; set; }

    /// <summary>Null when the instance had no assignee to take it from.</summary>
    public Guid? FromUserId { get; set; }

    public User FromUser { get; set; }

    public Guid ToUserId { get; set; }

    public User ToUser { get; set; }

    public Guid ReassignedById { get; set; }

    public User ReassignedBy { get; set; }

    public DateTime ReassignedAt { get; set; }

    [StringLength(1000)] public string Reason { get; set; }
}

/// <summary>
/// A plain audit record of a submitted worksheet being sent back to its analyst.
/// <para>
/// A first-class log rather than a "last return" field on the worksheet, and for the same
/// reason <see cref="WorksheetInstanceReassignment"/> is one: a worksheet can go round the
/// correction loop several times, and a mutable field would keep only the newest cycle while
/// silently discarding the ones before it. Every cycle is retained here.
/// </para>
/// <para>
/// Like a reassignment, this is not itself an electronic signature. When the return came
/// through the reviewer's signed decision the signature lives on its own
/// <see cref="QcApproval"/> round, and <see cref="ApprovalRound"/> is what ties this row to it.
/// </para>
/// </summary>
public class WorksheetInstanceCorrectionReturn : BaseEntity
{
    public Guid WorksheetInstanceId { get; set; }

    public WorksheetInstance WorksheetInstance { get; set; }

    public Guid ReturnedById { get; set; }

    public User ReturnedBy { get; set; }

    public DateTime ReturnedAt { get; set; }

    /// <summary>What the analyst has to act on. Mandatory, like a reason for change.</summary>
    [StringLength(1000)] public string Reason { get; set; }

    /// <summary>
    /// The review round this return interrupted — the <see cref="QcApproval.ApprovalRound"/>
    /// in force when it was sent back, so a return can be read against the signature trail it
    /// belongs to. Zero when the worksheet had no approval round yet.
    /// </summary>
    public int ApprovalRound { get; set; }

    /// <summary>
    /// True when the return came through the reviewer's signed decision (a declined
    /// <c>/review</c>, which records a re-authenticated <see cref="QcApproval"/> rejection);
    /// false when it came through the unsigned <c>/return-for-correction</c> action, which
    /// takes a reason but no credential.
    /// </summary>
    public bool Signed { get; set; }
}

/// <summary>
/// The <see cref="WorksheetFieldValue.ColumnKey"/> values the composite
/// Reagent/ReferenceStandard field shape uses.
/// <para>
/// Batch number and expiry are captured inline on every entry rather than looked up: the
/// existing <c>Reagent</c> catalog tracks only name and description, with no lot-level expiry
/// anywhere, and the real worksheets capture the expiry fresh each time too (confirmed against
/// the Cetrimide worksheet's "Date of Expiry" field). The hard expiry gate therefore checks
/// the value just entered, not a master-data record.
/// </para>
/// </summary>
public static class QcWorksheetValueColumns
{
    /// <summary>A <c>Reagent</c> id — a read-only catalog lookup.</summary>
    public const string ReagentId = "reagentId";

    /// <summary>Free text, entered per use.</summary>
    public const string BatchNo = "batchNo";

    /// <summary>A date, entered per use, and what the hard expiry gate checks against today.</summary>
    public const string ExpiryDate = "expiryDate";

    /// <summary>The three rows a single Reagent/ReferenceStandard entry is made of.</summary>
    public static readonly string[] All = [ReagentId, BatchNo, ExpiryDate];
}
