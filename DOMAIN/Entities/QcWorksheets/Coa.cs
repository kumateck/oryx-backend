using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.QcWorksheets;

/// <summary>
/// Which of the two certificate shapes one engine produced.
/// <para>
/// Derived from <see cref="TestRequest.Type"/> at generation time but <b>stored</b>, never
/// recomputed on read: a later change to the round-type taxonomy must not be able to silently
/// reshape a certificate that has already been issued.
/// </para>
/// </summary>
public enum CoaCertificateShape
{
    /// <summary>Material/Product/Water. Releases a batch, so it prints batch/manufacturing/expiry dates.</summary>
    CertificateOfAnalysis = 0,

    /// <summary>
    /// Routine Environmental. Reports on a room or point rather than releasing a batch, so it
    /// has no manufacturing or expiry date — not blank ones, none at all.
    /// </summary>
    EnvironmentalMonitoringReport = 1
}

/// <summary>
/// A certificate's lifecycle. Deliberately three-valued and append-only: there is no "Cancelled"
/// and no path back from <see cref="Issued"/> except being superseded by a revision.
/// </summary>
public enum CoaStatus
{
    Draft = 0,
    Issued = 1,

    /// <summary>
    /// A later revision has been issued in this one's place. The record stays fully retrievable
    /// and is never deleted or edited — that is the whole point of superseding rather than
    /// overwriting.
    /// </summary>
    Superseded = 2
}

/// <summary>
/// A Certificate of Analysis, or an Environmental Monitoring Report — one engine, two shapes.
/// <para>
/// <b>Bound to exactly one <see cref="TestRequest"/>.</b> A Specification's Chemical and
/// Microbial <see cref="SpecificationWorksheetLink"/>s already resolve into WorksheetInstances
/// under the same round, per (Subject × link), so a combined certificate never spans two rounds.
/// (An earlier draft of the domain model said "1, or 2 TestRequestIds"; that predated Milestone
/// 3's actual design and has been corrected.)
/// </para>
/// <para>
/// <b>Never authored.</b> There is no create endpoint and no edit endpoint. A Draft is produced
/// by the system the moment the round it certifies satisfies the strict-hold rule; the only two
/// user actions are issuing it and revising it.
/// </para>
/// <para>
/// <b>Fully snapshotted.</b> Every header value below, and every field on every
/// <see cref="CoaRow"/>, is copied in at generation time rather than joined live to the
/// Specification or the WorksheetInstances. A certificate is a regulatory document: once issued
/// its content must never change because somebody later revised the Specification or a stored
/// value's representation moved. Revision is the only path to different content, and it is
/// explicit, reasoned and versioned.
/// </para>
/// <para>
/// <b>Hard version pinning.</b> <see cref="SpecificationId"/> is the exact Specification version
/// row the round was pinned to, copied off <see cref="TestRequest.SpecificationId"/>, and
/// <see cref="SpecificationVersion"/> records which version that was without needing a join.
/// Nothing in this module walks <c>SupersedesId</c> forward, so a Specification revised after
/// this certificate was generated cannot change what it certifies.
/// </para>
/// <para>
/// Deliberately <b>not</b> <c>IRequireApproval</c>, and with no entry in
/// <see cref="QcWorksheetModelTypes"/>: issuing is not itself an Approval-chain action. Every
/// WorksheetInstance review that gated this certificate's generation already went through the
/// full re-authenticated Approval flow in Milestone 3, and stacking a second chain on top would
/// be a signature on a document nobody authored.
/// </para>
/// <para>
/// Entirely additive. This is a new table that neither reads nor writes
/// <c>CommercialCertificate</c>, <c>CommercialCoaItem</c> or <c>RoutineCertificate</c> — the
/// live Material/Product/Packaging certificate path keeps running unchanged alongside it.
/// </para>
/// </summary>
public class Coa : BaseEntity
{
    /// <summary>The one round this certificate is built from.</summary>
    public Guid TestRequestId { get; set; }

    public TestRequest TestRequest { get; set; }

    /// <summary>
    /// Allocated at generation, unique across every certificate. A revision takes its own code
    /// derived from the original's (<c>COA-2026-00842-R2</c>) rather than reusing it: both
    /// documents exist at once and a reader must never be unable to tell which is in their hand.
    /// </summary>
    [StringLength(100)] public string CertificateCode { get; set; }

    public CoaCertificateShape CertificateShape { get; set; }

    /// <summary>
    /// Self-referencing. Set on a revision, naming the certificate it replaces. The named
    /// certificate only actually moves to <see cref="CoaStatus.Superseded"/> when <i>this</i>
    /// one is issued — not when the revision is raised, because an unissued draft supersedes
    /// nothing.
    /// </summary>
    public Guid? SupersedesId { get; set; }

    public Coa Supersedes { get; set; }

    /// <summary>1 for the original, incrementing per revision. Printed as "Rev N" on the document.</summary>
    public int RevisionNumber { get; set; } = 1;

    /// <summary>
    /// Mandatory on a revision and null on an original: a certificate is only ever reissued for
    /// a stated reason (a retest completed, a data-entry correction was made). Enforced in
    /// <c>CoaRepository</c>, not by a database constraint — a conditional NOT NULL is not
    /// expressible here.
    /// </summary>
    [StringLength(1000)] public string RevisionReason { get; set; }

    public CoaStatus Status { get; set; } = CoaStatus.Draft;

    public DateTime? IssuedAt { get; set; }

    public Guid? IssuedById { get; set; }

    public User IssuedBy { get; set; }

    // --- Header block, snapshotted -----------------------------------------
    //
    // Computed once at generation and stored, for exactly the same reason CoaRow is snapshotted:
    // re-deriving the header on every view would let an issued certificate's face change when
    // the batch record or the Specification behind it was edited.

    /// <summary>
    /// The pinned Specification version row, copied from the round. Restrict-deleted, so the
    /// document the certificate was judged against stays readable for as long as it exists.
    /// </summary>
    public Guid SpecificationId { get; set; }

    public Specification Specification { get; set; }

    /// <summary>The pinned version number, so the header reads without a join.</summary>
    public int SpecificationVersion { get; set; }

    [StringLength(100)] public string SpecificationCode { get; set; }

    /// <summary>
    /// <see cref="CoaCertificateShape.CertificateOfAnalysis"/> only: the product or material the
    /// batch is of. Resolved at generation from the linked batch record, falling back to the
    /// Specification's own name when the round's Subject carries no batch link.
    /// </summary>
    [StringLength(500)] public string ProductOrMaterialName { get; set; }

    /// <summary>
    /// <see cref="CoaCertificateShape.CertificateOfAnalysis"/> only, and only for a round with a
    /// single Subject — which is what a Material/Product round is. Null on a multi-Subject round,
    /// where the batch/point identity belongs to each row-group rather than to the header.
    /// </summary>
    [StringLength(200)] public string BatchNumber { get; set; }

    /// <summary>
    /// <see cref="CoaCertificateShape.EnvironmentalMonitoringReport"/> only: the area covered.
    /// A single-room round names the room; a multi-room round names the count, because the rooms
    /// themselves are the row-groups.
    /// </summary>
    [StringLength(500)] public string AreaOrRoom { get; set; }

    /// <summary>
    /// CertificateOfAnalysis shape only. Stored nullable because a Material round's batch record
    /// may genuinely not carry the date; the Environmental shape never renders this field at all
    /// (see <c>EnvironmentalMonitoringReportHeaderDto</c>, which does not declare it).
    /// </summary>
    public DateTime? ManufacturingDate { get; set; }

    /// <summary>CertificateOfAnalysis shape only. See <see cref="ManufacturingDate"/>.</summary>
    public DateTime? ExpiryDate { get; set; }

    /// <summary>
    /// The sample's collection time. The earliest <c>CollectedAt</c> across the round's Subjects,
    /// since a multi-point round is sampled over a window rather than at an instant.
    /// </summary>
    public DateTime? SampleDate { get; set; }

    /// <summary>
    /// When testing actually finished: the latest review signature across every WorksheetInstance
    /// this certificate draws on.
    /// <para>
    /// Taken from the <see cref="QcApproval"/> signature trail rather than from
    /// <c>WorksheetInstance.UpdatedAt</c>, which moves again on every later write and so cannot
    /// stand for "when this was reviewed".
    /// </para>
    /// </summary>
    public DateTime? TestCompletionDate { get; set; }

    /// <summary>
    /// The whole-certificate verdict, snapshotted like everything else: true only when every row
    /// complies. Stored rather than computed from <see cref="Rows"/> on read so the overall
    /// result can never disagree with the rows as issued.
    /// </summary>
    public bool OverallComplies { get; set; }

    public List<CoaRow> Rows { get; set; } = [];
}

/// <summary>
/// One printed line of a certificate: a test, its criteria, the result, and whether it complied.
/// <para>
/// <b>Every field here is a snapshot.</b> Nothing on this row is joined live to a
/// <see cref="SpecificationCharacteristic"/> or a <see cref="WorksheetFieldValue"/> at read time.
/// The Specification could be superseded, or a Characteristic's wording corrected, the day after
/// this certificate was issued — and the certificate must still read exactly as it read when it
/// was signed.
/// </para>
/// </summary>
public class CoaRow : BaseEntity
{
    public Guid CoaId { get; set; }

    public Coa Coa { get; set; }

    /// <summary>
    /// Which Subject this row belongs to. A Material/Product round normally has one Subject and
    /// therefore one implicit row-group; a Water or Environmental round has many, one group per
    /// point or room — matching the real EM and Water certificates, which print one row-group per
    /// room.
    /// </summary>
    public Guid TestRequestSubjectId { get; set; }

    public TestRequestSubject TestRequestSubject { get; set; }

    /// <summary>
    /// The Subject's code, snapshotted. Beyond the brief's property list, and deliberately: a
    /// multi-Subject certificate prints its groups by this, and joining live to
    /// <see cref="TestRequestSubject"/> for the heading would reintroduce exactly the silent-drift
    /// problem the rest of this row exists to prevent.
    /// </summary>
    [StringLength(200)] public string SubjectRef { get; set; }

    /// <summary>The Subject's human name, snapshotted for the same reason as <see cref="SubjectRef"/>.</summary>
    [StringLength(500)] public string SubjectLabel { get; set; }

    /// <summary>
    /// The Characteristic this row was resolved from. Restrict-deleted and kept for traceability
    /// only — nothing reads through it to render the row, because every value it would supply is
    /// already snapshotted below.
    /// </summary>
    public Guid SpecificationCharacteristicId { get; set; }

    public SpecificationCharacteristic SpecificationCharacteristic { get; set; }

    /// <summary>
    /// The exact WorksheetInstance the result was taken from. Beyond the brief's property list,
    /// and deliberately: after an OOS disposition a Subject can hold both an original and a
    /// retest result, and "which one did this certificate draw from" is precisely the question
    /// lifecycle-and-governance.md requires the record to answer. Restrict-deleted.
    /// </summary>
    public Guid? SourceWorksheetInstanceId { get; set; }

    public WorksheetInstance SourceWorksheetInstance { get; set; }

    /// <summary>Snapshotted from <c>SpecificationCharacteristic.TestName</c>.</summary>
    [StringLength(255)] public string DisplayLabel { get; set; }

    /// <summary>Snapshotted from <c>SpecificationCharacteristic.GroupName</c> — the section heading, e.g. "CHEMICAL".</summary>
    [StringLength(200)] public string GroupName { get; set; }

    public int DisplayOrder { get; set; }

    /// <summary>Snapshotted, not joined live.</summary>
    [StringLength(2000)] public string AcceptanceCriteria { get; set; }

    /// <summary>
    /// Snapshotted from the resolved WorksheetInstance's field value at generation time. Stored
    /// as the text that was entered, never reformatted — the certificate prints what the analyst
    /// recorded.
    /// </summary>
    [StringLength(2000)] public string ResultValue { get; set; }

    /// <summary>
    /// The <c>LimitEvaluator</c> verdict on <see cref="ResultValue"/> against this row's own
    /// snapshotted limits, taken at generation time. An Alert-limit breach still complies: it
    /// flags for trend review and blocks nothing,
    /// which is the locked rule in lifecycle-and-governance.md.
    /// <para>
    /// Deliberately the <b>literal</b> evaluation of the value this row carries, and nothing else.
    /// A QA disposition never rewrites it — see <see cref="DispositionOutcome"/>, which annotates
    /// the row on top rather than altering what was measured. Keeping this boolean purely
    /// mechanical is what lets an auditor ask "did this value meet its limit" and get an answer no
    /// later human decision has edited.
    /// </para>
    /// </summary>
    public bool Complies { get; set; }

    /// <summary>
    /// The QA disposition governing this row, when a closed OOS case touched the value it carries.
    /// Null for the ordinary row, which is most of them.
    /// <para>
    /// This exists to stop a <i>resolved</i> finding from being misread as an unresolved one. When
    /// a case closed as <see cref="OosDispositionOutcome.Invalidated"/> with no retest to replace
    /// the result, the row still carries the literal value that was measured — never hidden, never
    /// blanked, because suppressing real data is exactly what a GxP record must not do — but the
    /// certificate reads it as "Invalidated", not as a bare failure. The two mean entirely
    /// different things to whoever is holding the document.
    /// </para>
    /// <para>
    /// Snapshotted at generation time like everything else here: the disposition in force when the
    /// certificate was produced is the one it keeps.
    /// </para>
    /// </summary>
    public OosDispositionOutcome? DispositionOutcome { get; set; }

    /// <summary>
    /// What QA said when they disposed of the case, snapshotted — the "[disposition reason]" the
    /// certificate prints beside the outcome, so a reader is not left with a bare label they would
    /// have to go and look up.
    /// </summary>
    [StringLength(2000)] public string DispositionReason { get; set; }
}

/// <summary>
/// How a row reads on the finished document, stated once so the row, its subject's section and the
/// certificate's overall verdict can never disagree with each other.
/// </summary>
public static class CoaRowVerdict
{
    /// <summary>
    /// Whether this row stands as a genuine, unresolved failure — which is the only kind that
    /// should drag a certificate's overall verdict down.
    /// <para>
    /// A row QA disposed of as <see cref="OosDispositionOutcome.Invalidated"/> (the result was
    /// void) or <see cref="OosDispositionOutcome.RetestAccepted"/> (a different result counts) is
    /// resolved: the finding was investigated and closed, and the batch was released on the
    /// strength of that decision. <see cref="OosDispositionOutcome.ConfirmedOOS"/> is the
    /// opposite — QA confirmed the result stands and the batch was rejected — so it counts.
    /// </para>
    /// <para>
    /// <see cref="CoaRow.Complies"/> itself is never touched by any of this. It stays the literal
    /// evaluation of the measured value, for audit.
    /// </para>
    /// </summary>
    public static bool IsUnresolvedFailure(CoaRow row)
    {
        ArgumentNullException.ThrowIfNull(row);

        if (row.Complies)
            return false;

        return row.DispositionOutcome is not (OosDispositionOutcome.Invalidated
            or OosDispositionOutcome.RetestAccepted);
    }

    /// <summary>
    /// What the compliance column prints.
    /// <para>
    /// A disposed row never reads as a bare "Does not comply": that phrasing belongs to a finding
    /// nobody has resolved, and printing it over a closed investigation would misrepresent the
    /// record to whoever is holding the certificate. It names the disposition instead, with QA's
    /// own words beside it, while the measured value stays visible in its own column.
    /// </para>
    /// </summary>
    public static string Label(CoaRow row)
    {
        ArgumentNullException.ThrowIfNull(row);

        if (row.DispositionOutcome is null)
            return row.Complies ? "Complies" : "Does not comply";

        var outcome = row.DispositionOutcome.Value switch
        {
            OosDispositionOutcome.Invalidated => "Invalidated",
            OosDispositionOutcome.RetestAccepted => "Retest accepted",
            OosDispositionOutcome.ConfirmedOOS => "Confirmed out of specification",
            _ => row.Complies ? "Complies" : "Does not comply"
        };

        return string.IsNullOrWhiteSpace(row.DispositionReason)
            ? outcome
            : $"{outcome} — {row.DispositionReason.Trim()}";
    }
}
