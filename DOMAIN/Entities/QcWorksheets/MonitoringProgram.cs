using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.QcWorksheets;

/// <summary>
/// Which routine discipline a sampling point belongs to.
/// <para>
/// Deliberately a two-valued enum of its own rather than a reuse of
/// <see cref="TestRequestType"/> or <see cref="SpecificationAppliesTo"/>: a sampling point is
/// only ever Water or Environmental, and the three batch-bearing categories are not merely
/// unused here — they are meaningless. <see cref="QcSamplingPointTypes"/> holds the one mapping
/// onto the round taxonomy, the same way <see cref="QcTestRequestTypes"/> holds the one mapping
/// between a round and its Specification's <see cref="SpecificationAppliesTo"/>.
/// </para>
/// </summary>
public enum SamplingPointType
{
    Water = 0,
    Environmental = 1
}

/// <summary>How often a <see cref="MonitoringProgram"/> comes round.</summary>
public enum MonitoringFrequency
{
    Daily = 0,
    Weekly = 1,
    Monthly = 2,
    Quarterly = 3,

    /// <summary>Interval given by <see cref="MonitoringProgram.CustomIntervalDays"/>.</summary>
    Custom = 4
}

/// <summary>
/// Whether the schedule is running.
/// <para>
/// Deliberately two-valued and with no "Retired": a program that must stop permanently is
/// deleted (soft-deleted, like every other record here), while Paused is the reversible state
/// the Pause/Resume pair moves between. A third terminal state would be a second way of
/// spelling "not scheduled" with no behavioural difference from the first.
/// </para>
/// </summary>
public enum MonitoringProgramStatus
{
    Active = 0,
    Paused = 1
}

/// <summary>
/// Maps a sampling point's discipline onto the round type and Specification category that
/// govern it. One mapping, stated once, so the three taxonomies cannot drift apart.
/// </summary>
public static class QcSamplingPointTypes
{
    public static TestRequestType ToTestRequestType(SamplingPointType type) => type switch
    {
        SamplingPointType.Water => TestRequestType.RoutineWater,
        SamplingPointType.Environmental => TestRequestType.RoutineEnvironmental,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    public static SpecificationAppliesTo ToAppliesTo(SamplingPointType type) =>
        QcTestRequestTypes.ToAppliesTo(ToTestRequestType(type));

    /// <summary>The sampling point discipline a routine round covers, or null for a batch round.</summary>
    public static SamplingPointType? FromTestRequestType(TestRequestType type) => type switch
    {
        TestRequestType.RoutineWater => SamplingPointType.Water,
        TestRequestType.RoutineEnvironmental => SamplingPointType.Environmental,
        _ => null
    };
}

/// <summary>
/// A physical point that gets sampled — a water outlet, a room, a booth.
/// <para>
/// Master data, and the correction this milestone makes to the old system, where a sampling
/// point was only ever a loose string. A typed code silently creates a second point on every
/// typo, which splits that point's trend history in two and applies whichever Alert/Action tier
/// the typo happened to resolve to — the same failure mode
/// <see cref="SamplingPointGroup"/> was introduced in Milestone 2 to prevent, one level down.
/// </para>
/// <para>
/// Plain reference data: no lifecycle, no versioning, no approval. It is not a controlled
/// document, so it carries none of the <see cref="QcDocumentStatus"/> machinery.
/// </para>
/// </summary>
public class SamplingPoint : BaseEntity
{
    /// <summary>The point's code, e.g. "SF-91". Unique among live points.</summary>
    [StringLength(100)] public string Code { get; set; }

    [StringLength(500)] public string Name { get; set; }

    /// <summary>The area or block the point sits in, e.g. "Granulation". Free text; not a department FK.</summary>
    [StringLength(200)] public string Area { get; set; }

    public SamplingPointType Type { get; set; }

    /// <summary>
    /// Which Alert/Action tier applies to results from this point — the same table Milestone 2's
    /// <see cref="SpecificationCharacteristic"/> groups its tiers by, referenced rather than
    /// duplicated.
    /// <para>
    /// Optional, because a point only needs a group when its Specification's Characteristics
    /// actually use grouped limits. An Environmental Specification that carries one flat limit
    /// for every room has nothing to tier, and forcing a group on it would invent a distinction
    /// that does not exist.
    /// </para>
    /// </summary>
    public Guid? SamplingPointGroupId { get; set; }

    public SamplingPointGroup SamplingPointGroup { get; set; }
}

/// <summary>
/// The schedule for one sampling point: how often it is tested, against what, and when it is
/// next due.
/// <para>
/// <b>One point per program</b> is a locked decision about <i>configuration</i> granularity, not
/// about execution. Each point's frequency, Specification and pause state are independently
/// manageable, which is what "one point per program" buys. It emphatically does not mean one
/// <see cref="TestRequest"/> per program: a real Environmental round covers 60–90 rooms and a
/// Water round 15+ points in a single physical sweep, and
/// <c>QcMonitoringScanService</c> reconciles the two by grouping every program due the same day
/// that shares a Type and a Specification into <b>one</b> round with one
/// <see cref="TestRequestSubject"/> per point.
/// </para>
/// <para>
/// Operational configuration, not a controlled document — so there is no Draft/Approved/
/// Effective lifecycle, no version chain and no approval. Editing one is an ordinary edit.
/// </para>
/// </summary>
public class MonitoringProgram : BaseEntity
{
    /// <summary>The one point this program schedules. Locked at one per program.</summary>
    public Guid SamplingPointId { get; set; }

    public SamplingPoint SamplingPoint { get; set; }

    /// <summary>
    /// Singular, matching <see cref="TestRequest.SpecificationId"/>'s own design: the linked
    /// Specification's <see cref="Specification.WorksheetLinks"/> already determine whether the
    /// round runs Chemical, Microbial or both, so a program has nothing to track separately.
    /// <para>
    /// Each Specification version is its own row, so this id already names one exact version.
    /// </para>
    /// </summary>
    public Guid SpecificationId { get; set; }

    public Specification Specification { get; set; }

    /// <summary>
    /// The pinned version number, captured server-side from the Specification row whenever the
    /// program is created or repointed. Never client-supplied.
    /// <para>
    /// <b>Hard version pinning</b> (lifecycle-and-governance.md, "Version pinning"), applied here
    /// exactly as it is to <see cref="TestRequest"/>, <see cref="WorksheetInstance"/>,
    /// <see cref="SpecificationWorksheetLink"/> and <see cref="Coa"/>: nothing in this module
    /// resolves a Specification forward, and this program is no exception. When the Specification
    /// it names is superseded, this program does <i>not</i> silently start scheduling against the
    /// successor — someone repoints it, as an ordinary edit of operational configuration, and the
    /// scan refuses to raise rounds against a no-longer-Effective document in the meantime (see
    /// <c>QcMonitoringScanService</c>). The alternative — walking <c>SupersedesId</c> forward at
    /// generation time — would be the first forward resolution anywhere in this module, and would
    /// let a Specification revision change what a schedule tests without anyone deciding so.
    /// </para>
    /// </summary>
    public int SpecificationVersion { get; set; }

    public MonitoringFrequency Frequency { get; set; }

    /// <summary>
    /// Required, and meaningful only, when <see cref="Frequency"/> is
    /// <see cref="MonitoringFrequency.Custom"/> — enforced in <c>MonitoringProgramRepository</c>
    /// rather than by a database constraint, since a conditional NOT NULL is not expressible
    /// here and the rule belongs beside the rest of the program's validation.
    /// </summary>
    public int? CustomIntervalDays { get; set; }

    /// <summary>
    /// How many days before the due date the round should be raised, so sampling and incubation
    /// can start on time. Zero means "raise it on the day".
    /// </summary>
    public int LeadTimeDays { get; set; }

    /// <summary>
    /// When this point is next due.
    /// <para>
    /// Advanced <b>at generation time, not completion time</b>. The scan advances it in the same
    /// transaction that creates the round, so the same day's second run — or the next day's run
    /// while the round is still in the lab — cannot raise a duplicate for an occurrence that has
    /// already been generated. Tying the advance to completion instead would make the schedule
    /// depend on lab turnaround, and a slow round would silently suppress the next one.
    /// </para>
    /// </summary>
    public DateTime NextDueDate { get; set; }

    public MonitoringProgramStatus Status { get; set; } = MonitoringProgramStatus.Active;
}
