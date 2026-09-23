using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.QcWorksheets;

/// <summary>
/// A water validity window's lifecycle.
/// <para>
/// <see cref="PendingActivation"/> is the whole point of this enum. A window is created by the
/// system the moment a Water certificate is issued, but production may not rely on it until a
/// named person has confirmed, with a stated reason, that the backdated coverage is justified —
/// see <see cref="WaterQualityPeriod"/>.
/// </para>
/// </summary>
public enum WaterQualityPeriodStatus
{
    /// <summary>Scaffolded by certificate issuance. Covers nothing until explicitly activated.</summary>
    PendingActivation = 0,

    Active = 1,

    /// <summary>
    /// Withdrawn. Every <see cref="WaterUseRecord"/> underneath it is flagged for Quality Impact
    /// Assessment in the same transaction — nothing depends on anyone remembering to check.
    /// </summary>
    Held = 2,

    /// <summary>
    /// <see cref="WaterQualityPeriod.ValidUntil"/> has passed without the next round's window
    /// taking over. Set by the daily scan rather than inferred on read, so a delayed next round
    /// actively flags the old window as no-longer-current instead of silently continuing to
    /// cover production (lifecycle-and-governance.md, "Water validity periods").
    /// </summary>
    Expired = 3
}

/// <summary>Whether a recorded water use is still clean, or caught by its window being held.</summary>
public enum WaterUseRecordStatus
{
    Recorded = 0,

    /// <summary>
    /// Flagged for Quality Impact Assessment because its window was held. The flag is all this
    /// milestone models: the QIA investigation itself is a QA process, not a QC data question,
    /// and is deliberately not modelled anywhere in this design.
    /// </summary>
    Held = 1
}

/// <summary>
/// The window during which a sampling point's water was certified fit for production use.
/// <para>
/// Water is the one category consumed continuously between tests, which is why it carries a
/// validity window the batch categories do not need.
/// </para>
/// <para>
/// <b>Retroactive by construction.</b> <see cref="ValidFrom"/> is always the sample's own
/// collection timestamp, never the activation timestamp: microbial results take days to
/// incubate, so by the time anyone can say the water was good, production has already been using
/// it for the whole incubation window. Backdating is what makes the record true. That is also
/// why <see cref="RetrospectiveReason"/> is not conditional — there is always a backdated gap to
/// justify.
/// </para>
/// <para>
/// <b>Dynamic <see cref="ValidUntil"/>.</b> Set on activation to the point's next scheduled
/// <see cref="MonitoringProgram.NextDueDate"/>, not to a fixed duration from
/// <see cref="ValidFrom"/>. A delayed next round therefore leaves a window that visibly expires
/// rather than one that quietly keeps covering production.
/// </para>
/// <para>
/// <b>Created inert.</b> Issuing a Water certificate creates this row at
/// <see cref="WaterQualityPeriodStatus.PendingActivation"/> with no
/// <see cref="ValidUntil"/> at all. It is scaffolding: it covers nothing, and no
/// <see cref="WaterUseRecord"/> can be booked against it. Activation is a separate, reasoned,
/// permissioned action, deliberately not automatic — telling production it may rely on water for
/// a window that has already elapsed is a decision a person makes.
/// </para>
/// </summary>
public class WaterQualityPeriod : BaseEntity
{
    public Guid SamplingPointId { get; set; }

    public SamplingPoint SamplingPoint { get; set; }

    /// <summary>The Subject whose Reviewed, certified results back this window.</summary>
    public Guid TestRequestSubjectId { get; set; }

    public TestRequestSubject TestRequestSubject { get; set; }

    /// <summary>
    /// Always <see cref="TestRequestSubject.CollectedAt"/>. Copied in at creation rather than
    /// joined on read, for the same reason a certificate snapshots everything it prints: what
    /// this window covered must not change because a Subject was later corrected.
    /// </summary>
    public DateTime ValidFrom { get; set; }

    /// <summary>
    /// Null until activation, and then the point's next scheduled test date as it stood at that
    /// moment. Null is not "open-ended" — a window with no end covers nothing, because it is not
    /// yet Active.
    /// </summary>
    public DateTime? ValidUntil { get; set; }

    /// <summary>
    /// Why the backdated coverage is justified, captured from the activating user.
    /// <para>
    /// Mandatory on activation and enforced in <c>WaterQualityRepository</c> rather than by a
    /// NOT NULL column — exactly as <see cref="TestRequest.UnscheduledReason"/> is. A row spends
    /// its first phase of life at <see cref="WaterQualityPeriodStatus.PendingActivation"/>, where
    /// the system has created it and no human has yet been asked anything, so there is no reason
    /// to store. Seeding a placeholder to satisfy a database constraint would put system-authored
    /// text in a field whose entire value is that a person wrote it.
    /// </para>
    /// <para>
    /// Written once, at activation, and never rewritten afterwards: a hold captures its own
    /// reason in <see cref="HoldReason"/> rather than overwriting this one.
    /// </para>
    /// </summary>
    [StringLength(1000)] public string RetrospectiveReason { get; set; }

    public WaterQualityPeriodStatus Status { get; set; } = WaterQualityPeriodStatus.PendingActivation;

    public Guid? ActivatedById { get; set; }

    public User ActivatedBy { get; set; }

    public DateTime? ActivatedAt { get; set; }

    public Guid? HeldById { get; set; }

    public User HeldBy { get; set; }

    public DateTime? HeldAt { get; set; }

    [StringLength(1000)] public string HoldReason { get; set; }

    public List<WaterUseRecord> UseRecords { get; set; } = [];
}

/// <summary>
/// One recorded consumption of water covered by a <see cref="WaterQualityPeriod"/>.
/// <para>
/// <b>Manual entry only in this milestone.</b> Automatic capture from production consumption was
/// explicitly deferred (deferred-and-next-steps.md, carried forward from the superseded routine
/// implementation's own follow-up boundary) and stays deferred: nothing here hooks a production
/// step, and this table is written by one endpoint a person calls.
/// </para>
/// <para>
/// A record exists so that holding a window can name exactly what it touched. That is its whole
/// purpose, which is why it may only ever be booked against an <see cref="WaterQualityPeriodStatus.Active"/>
/// window — a use recorded against scaffolding or against an already-withdrawn window asserts
/// coverage that was never granted.
/// </para>
/// </summary>
public class WaterUseRecord : BaseEntity
{
    public Guid WaterQualityPeriodId { get; set; }

    public WaterQualityPeriod WaterQualityPeriod { get; set; }

    public DateTime UsedAt { get; set; }

    /// <summary>The batch that consumed the water, when the use is attributable to one.</summary>
    public Guid? BatchManufacturingRecordId { get; set; }

    public BatchManufacturingRecord BatchManufacturingRecord { get; set; }

    /// <summary>The production step that consumed it, when the use is attributable to one.</summary>
    public Guid? ProductionActivityStepId { get; set; }

    public ProductionActivityStep ProductionActivityStep { get; set; }

    /// <summary>Who booked the use. Required — an unattributed consumption record proves nothing.</summary>
    public Guid RecordedById { get; set; }

    public User RecordedBy { get; set; }

    public WaterUseRecordStatus Status { get; set; } = WaterUseRecordStatus.Recorded;
}
