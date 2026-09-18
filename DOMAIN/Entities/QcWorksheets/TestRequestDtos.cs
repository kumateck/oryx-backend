using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.QcWorksheets;

// ---------------------------------------------------------------------------
// TestRequest — reads
// ---------------------------------------------------------------------------

public class TestRequestSummaryDto : BaseDto
{
    public TestRequestType Type { get; set; }

    public Guid SpecificationId { get; set; }

    /// <summary>
    /// The pinned Specification version this round runs against — never the version that
    /// happens to be Effective when it is read.
    /// </summary>
    public int SpecificationVersion { get; set; }

    public string SpecificationCode { get; set; }
    public string SpecificationName { get; set; }

    public TestRequestScheduleOrigin ScheduleOrigin { get; set; }
    public string UnscheduledReason { get; set; }
    public string ArNumber { get; set; }
    public string IssueNumber { get; set; }
    public DateTime? IssuedAt { get; set; }
    public UserDto IssuedBy { get; set; }
    public TestRequestStatus Status { get; set; }

    public int SubjectCount { get; set; }
    public int WorksheetInstanceCount { get; set; }
}

public class TestRequestDetailDto : TestRequestSummaryDto
{
    public List<TestRequestSubjectDto> Subjects { get; set; } = [];
}

public class TestRequestSubjectDto : BaseDto
{
    public Guid TestRequestId { get; set; }
    public string SubjectRef { get; set; }
    public string SubjectLabel { get; set; }
    public string ArNumber { get; set; }
    public Guid? SamplingPointGroupId { get; set; }
    public SamplingPointGroupDto SamplingPointGroup { get; set; }

    /// <summary>
    /// Populated only by Milestone 6's MonitoringProgram generation; no endpoint in this
    /// milestone accepts it.
    /// </summary>
    public Guid? SamplingPointId { get; set; }

    public Guid? MaterialBatchId { get; set; }
    public Guid? BatchManufacturingRecordId { get; set; }
    public DateTime? CollectedAt { get; set; }

    public List<WorksheetInstanceSummaryDto> WorksheetInstances { get; set; } = [];
}

public class WorksheetInstanceSummaryDto : BaseDto
{
    public Guid TestRequestSubjectId { get; set; }
    public Guid WorksheetTemplateId { get; set; }

    /// <summary>The pinned template version this instance renders and records against.</summary>
    public int WorksheetTemplateVersion { get; set; }

    public string WorksheetTemplateCode { get; set; }
    public string WorksheetTemplateName { get; set; }
    public SpecificationAnalysisType AnalysisType { get; set; }

    public Guid? AssignedToId { get; set; }
    public UserDto AssignedTo { get; set; }
    public Guid? AssignedById { get; set; }
    public DateTime? AssignedAt { get; set; }

    public WorksheetInstanceStatus Status { get; set; }
    public bool Approved { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public Guid? RetestOfInstanceId { get; set; }
}

// ---------------------------------------------------------------------------
// TestRequest — writes
// ---------------------------------------------------------------------------

/// <summary>
/// Every enum is nullable and <c>[Required]</c> for the same reason Milestone 2's requests
/// are: an omitted enum would otherwise bind to the CLR zero value and silently pick
/// RawMaterial/Scheduled rather than being rejected.
/// </summary>
public class CreateTestRequestRequest
{
    [Required] public TestRequestType? Type { get; set; }

    /// <summary>
    /// The Specification version row to pin to. The version number itself is read server-side
    /// from this row and is deliberately not accepted from the client — a request carrying its
    /// own version could otherwise claim a pin that does not match the row it names.
    /// </summary>
    [Required] public Guid SpecificationId { get; set; }

    [Required] public TestRequestScheduleOrigin? ScheduleOrigin { get; set; }

    /// <summary>Mandatory when <see cref="ScheduleOrigin"/> is Unscheduled.</summary>
    [StringLength(1000)] public string UnscheduledReason { get; set; }

    [Required, StringLength(100)] public string ArNumber { get; set; }

    [StringLength(100)] public string IssueNumber { get; set; }

    public DateTime? IssuedAt { get; set; }

    public List<CreateTestRequestSubjectRequest> Subjects { get; set; } = [];
}

public class CreateTestRequestSubjectRequest
{
    [Required, StringLength(200)] public string SubjectRef { get; set; }
    [StringLength(500)] public string SubjectLabel { get; set; }
    [StringLength(100)] public string ArNumber { get; set; }

    /// <summary>Water/EM only; an id from the SamplingPointGroup table, never free text.</summary>
    public Guid? SamplingPointGroupId { get; set; }

    /// <summary>RawMaterial/PackagingMaterial only.</summary>
    public Guid? MaterialBatchId { get; set; }

    /// <summary>Product only.</summary>
    public Guid? BatchManufacturingRecordId { get; set; }

    public DateTime? CollectedAt { get; set; }
}

/// <summary>
/// Adds Subjects to a round that has not started testing — the EM/Water case where points are
/// added incrementally. Each new Subject gets its own WorksheetInstances off the round's
/// already-pinned Specification version.
/// </summary>
public class AddTestRequestSubjectsRequest
{
    [Required, MinLength(1)] public List<CreateTestRequestSubjectRequest> Subjects { get; set; } = [];
}

/// <summary>
/// Records collection against one or more Subjects of a round. An empty
/// <see cref="SubjectIds"/> means every Subject of the round.
/// </summary>
public class RecordTestRequestSampleRequest
{
    public List<Guid> SubjectIds { get; set; } = [];

    /// <summary>Defaults to now when omitted.</summary>
    public DateTime? CollectedAt { get; set; }
}

// ---------------------------------------------------------------------------
// WorksheetInstance — reads
// ---------------------------------------------------------------------------

/// <summary>
/// The fixed header every real filled ARD prints. Computed on read, never stored and never
/// analyst-entered — there is no write path to any value on this type.
/// </summary>
public class WorksheetInstanceHeaderDto
{
    public Guid TestRequestId { get; set; }
    public TestRequestType TestRequestType { get; set; }

    /// <summary>Batch number, or sampling point code — from the Subject, never typed here.</summary>
    public string SubjectRef { get; set; }

    public string SubjectLabel { get; set; }

    /// <summary>The Subject's own AR sub-number, falling back to the round's AR number.</summary>
    public string ArNumber { get; set; }

    public string SpecificationCode { get; set; }

    /// <summary>The PINNED Specification version, not whichever is currently Effective.</summary>
    public int SpecificationVersion { get; set; }

    /// <summary>Rendered "Rev N" exactly as the paper header prints it.</summary>
    public string SpecificationRevision { get; set; }

    /// <summary>Resolved through the PINNED worksheet template version's StpId.</summary>
    public string StpCode { get; set; }

    public Guid? StpId { get; set; }

    public string WorksheetTemplateCode { get; set; }
    public string WorksheetTemplateName { get; set; }
    public int WorksheetTemplateVersion { get; set; }

    public string IssueNumber { get; set; }
    public DateTime? IssuedAt { get; set; }
    public UserDto IssuedBy { get; set; }

    /// <summary>
    /// Material/Product only, resolved read-only from the linked batch record; blank for
    /// Water/EM, which have no manufacturing or expiry date.
    /// </summary>
    public DateTime? ManufacturingDate { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public DateTime? SampledOn { get; set; }

    /// <summary>
    /// The analysis window. It opens when the test first entered someone's hands — the earlier
    /// of the current assignment and the first reassignment, so a reassigned test still
    /// reports when work actually began — and closes at submission.
    /// </summary>
    public DateTime? AnalysisDateFrom { get; set; }

    public DateTime? AnalysisDateTo { get; set; }
}

public class WorksheetInstanceDetailDto : WorksheetInstanceSummaryDto
{
    public WorksheetInstanceHeaderDto Header { get; set; }
    public List<WorksheetInstanceSectionDto> Sections { get; set; } = [];
    public List<WorksheetInstanceReassignmentDto> Reassignments { get; set; } = [];

    /// <summary>
    /// Every correction cycle this worksheet went through, oldest first — not just the most
    /// recent one.
    /// </summary>
    public List<WorksheetInstanceCorrectionReturnDto> CorrectionReturns { get; set; } = [];
}

public class WorksheetInstanceSectionDto
{
    public Guid Id { get; set; }
    public int Order { get; set; }
    public string Name { get; set; }
    public Guid? InstrumentId { get; set; }
    public List<WorksheetInstanceFieldDto> Fields { get; set; } = [];
}

/// <summary>A template field merged with whatever this instance has recorded against it.</summary>
public class WorksheetInstanceFieldDto
{
    public Guid Id { get; set; }
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

    /// <summary>
    /// True for anything the analyst may not type into: a Constant-mode field, a Calculated
    /// field, and a ReferencedResult field (which is resolved, never entered).
    /// </summary>
    public bool ReadOnly { get; set; }

    /// <summary>
    /// True for a field <c>/submit</c> requires a value on — every Entry-mode field that
    /// captures data, plus every ReferencedResult field, which must have resolved.
    /// </summary>
    public bool RequiredForSubmission { get; set; }

    public List<WorksheetFieldValueDto> Values { get; set; } = [];

    /// <summary>Present only on a ReferencedResult field.</summary>
    public ReferencedResultDto ReferencedResult { get; set; }
}

public class WorksheetFieldValueDto : BaseDto
{
    public string FieldKey { get; set; }
    public int? RowIndex { get; set; }
    public string ColumnKey { get; set; }
    public string Value { get; set; }
    public Guid EnteredById { get; set; }
    public UserDto EnteredBy { get; set; }
    public DateTime EnteredAt { get; set; }
    public Guid? ResolvedFromInstanceId { get; set; }
}

/// <summary>
/// The runtime resolution of a ReferencedResult field. An unresolved field is rendered as
/// pending, not as an error — the analyst may simply not have entered the resolution key yet —
/// but it cannot be submitted until it resolves.
/// </summary>
public class ReferencedResultDto
{
    public Guid? SourceTemplateId { get; set; }
    public string SourceTemplateCode { get; set; }
    public string SourceFieldKey { get; set; }

    /// <summary>The field on this instance whose entered value is the lookup key.</summary>
    public string ResolutionFieldKey { get; set; }

    /// <summary>The lookup value read from that field, or null when it has not been entered yet.</summary>
    public string ResolutionValue { get; set; }

    public bool Resolved { get; set; }

    /// <summary>The source instance the value came from — a clickable link in the Test Room.</summary>
    public Guid? ResolvedFromInstanceId { get; set; }

    public string Value { get; set; }

    /// <summary>Why it has not resolved, phrased for the analyst.</summary>
    public string Message { get; set; }
}

public class WorksheetInstanceReassignmentDto : BaseDto
{
    public Guid WorksheetInstanceId { get; set; }
    public Guid? FromUserId { get; set; }
    public UserDto FromUser { get; set; }
    public Guid ToUserId { get; set; }
    public UserDto ToUser { get; set; }
    public Guid ReassignedById { get; set; }
    public UserDto ReassignedBy { get; set; }
    public DateTime ReassignedAt { get; set; }
    public string Reason { get; set; }
}

/// <summary>
/// One correction cycle: who sent the worksheet back, when, why, and which review round it
/// interrupted.
/// </summary>
public class WorksheetInstanceCorrectionReturnDto : BaseDto
{
    public Guid WorksheetInstanceId { get; set; }
    public Guid ReturnedById { get; set; }
    public UserDto ReturnedBy { get; set; }
    public DateTime ReturnedAt { get; set; }
    public string Reason { get; set; }

    /// <summary>The QcApproval round in force when the worksheet was sent back.</summary>
    public int ApprovalRound { get; set; }

    /// <summary>
    /// True when the return came through the reviewer's re-authenticated decision, false when
    /// it came through the unsigned return-for-correction action.
    /// </summary>
    public bool Signed { get; set; }
}

/// <summary>
/// One card in the Test Room or the review queue: the instance plus just enough of its round
/// to render without a second call.
/// </summary>
public class WorksheetQueueItemDto : WorksheetInstanceSummaryDto
{
    public Guid TestRequestId { get; set; }
    public TestRequestType TestRequestType { get; set; }
    public string TestRequestArNumber { get; set; }
    public string SubjectRef { get; set; }
    public string SubjectLabel { get; set; }
    public string SubjectArNumber { get; set; }
    public DateTime? CollectedAt { get; set; }
    public string SpecificationCode { get; set; }
    public int SpecificationVersion { get; set; }

    /// <summary>
    /// True when this worksheet's submission triggered an OOS case that is still open. The
    /// reviewer queue renders these with a red flag and a link into the case
    /// (test-room-ux.md, "Reviewer queue") — a reviewer has to see that a result is already
    /// under formal investigation before they sign it off.
    /// </summary>
    public bool HasOpenOosCase { get; set; }

    /// <summary>The case to link to, when there is one. Null otherwise.</summary>
    public Guid? OosCaseId { get; set; }
}

/// <summary>The Test Room's three counts, alongside the cards themselves.</summary>
public class WorksheetQueueDto
{
    public int PendingCount { get; set; }
    public int InProgressCount { get; set; }
    public int AwaitingReviewCount { get; set; }
    public List<WorksheetQueueItemDto> Items { get; set; } = [];
}

// ---------------------------------------------------------------------------
// WorksheetInstance — writes
// ---------------------------------------------------------------------------

public class AssignWorksheetInstanceRequest
{
    [Required] public Guid AssignedToId { get; set; }
}

/// <summary>
/// Reassignment is audited, not signed: it carries a reason, but no re-authentication
/// credential, because moving work between analysts is administrative rather than a
/// meaning-of-signature event.
/// </summary>
public class ReassignWorksheetInstanceRequest
{
    [Required] public Guid AssignedToId { get; set; }
    [Required, StringLength(1000)] public string Reason { get; set; }
}

public class SaveWorksheetValuesRequest
{
    [Required] public List<WorksheetFieldValueEntry> FieldValues { get; set; } = [];
}

/// <summary>
/// One value being written. <see cref="EnteredById"/>/<c>EnteredAt</c> are deliberately absent:
/// attribution is taken from the authenticated caller, never from the payload.
/// </summary>
public class WorksheetFieldValueEntry
{
    [Required, StringLength(100)] public string FieldKey { get; set; }

    /// <summary>Null for a scalar field; the 0-based row when the field is a Table.</summary>
    public int? RowIndex { get; set; }

    /// <summary>Null for a plain scalar; a Table column key, or a Reagent sub-value key.</summary>
    [StringLength(100)] public string ColumnKey { get; set; }

    public string Value { get; set; }
}

/// <summary>
/// The reviewer's decision. <see cref="Approve"/> = false returns the worksheet for
/// correction, which is why <see cref="Comments"/> becomes mandatory in that case: it is the
/// correction reason the analyst acts on.
/// </summary>
public class ReviewWorksheetInstanceRequest
{
    [Required] public bool Approve { get; set; }

    /// <summary>The caller's own password — the QC re-authentication wrapper's credential.</summary>
    [Required] public string Password { get; set; }

    [StringLength(1000)] public string Comments { get; set; }
}

public class ReturnWorksheetForCorrectionRequest
{
    [Required, StringLength(1000)] public string Reason { get; set; }
}
