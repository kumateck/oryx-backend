using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.QcWorksheets;

// ---------------------------------------------------------------------------
// Sampling points
// ---------------------------------------------------------------------------

public class SamplingPointDto : BaseDto
{
    public string Code { get; set; }
    public string Name { get; set; }
    public string Area { get; set; }
    public SamplingPointType Type { get; set; }
    public Guid? SamplingPointGroupId { get; set; }
    public SamplingPointGroupDto SamplingPointGroup { get; set; }
}

/// <summary>
/// <see cref="Type"/> is nullable and <c>[Required]</c> on purpose, the same way every other
/// enum on a QC create request is: an omitted enum binds to the CLR zero value, which would
/// silently file every point as Water.
/// </summary>
public class CreateSamplingPointRequest
{
    [Required, StringLength(100)] public string Code { get; set; }
    [Required, StringLength(500)] public string Name { get; set; }
    [StringLength(200)] public string Area { get; set; }
    [Required] public SamplingPointType? Type { get; set; }
    public Guid? SamplingPointGroupId { get; set; }
}

public class UpdateSamplingPointRequest : CreateSamplingPointRequest;

// ---------------------------------------------------------------------------
// Monitoring programs
// ---------------------------------------------------------------------------

public class MonitoringProgramDto : BaseDto
{
    public Guid SamplingPointId { get; set; }
    public SamplingPointDto SamplingPoint { get; set; }

    public Guid SpecificationId { get; set; }

    /// <summary>
    /// Reported as stored, never re-read from the Specification row — a pin that had somehow
    /// drifted would be visible here rather than papered over on the way out.
    /// </summary>
    public int SpecificationVersion { get; set; }

    public string SpecificationCode { get; set; }
    public string SpecificationName { get; set; }

    public MonitoringFrequency Frequency { get; set; }
    public int? CustomIntervalDays { get; set; }
    public int LeadTimeDays { get; set; }
    public DateTime NextDueDate { get; set; }
    public MonitoringProgramStatus Status { get; set; }

    /// <summary>
    /// Which bucket the calendar view files this program under — Overdue / Due Today /
    /// Due This Week / Scheduled. Computed on read from <see cref="NextDueDate"/> rather than
    /// stored: it is a rendering of today's date against the schedule, not a fact about the
    /// program, and storing it would make it wrong the moment the clock moved.
    /// </summary>
    public MonitoringDueBucket DueBucket { get; set; }
}

/// <summary>The calendar view's groupings (test-room-ux.md).</summary>
public enum MonitoringDueBucket
{
    Overdue = 0,
    DueToday = 1,
    DueThisWeek = 2,
    Scheduled = 3,

    /// <summary>Paused programs are not due at all, whatever their NextDueDate says.</summary>
    Paused = 4
}

public class CreateMonitoringProgramRequest
{
    [Required] public Guid SamplingPointId { get; set; }

    /// <summary>
    /// The exact Specification version row this program pins to. Not a lineage id: each version
    /// is its own row, and nothing in this module resolves one forward.
    /// </summary>
    [Required] public Guid SpecificationId { get; set; }

    [Required] public MonitoringFrequency? Frequency { get; set; }

    /// <summary>Required when <see cref="Frequency"/> is Custom, and rejected otherwise.</summary>
    [Range(1, 3650)] public int? CustomIntervalDays { get; set; }

    [Range(0, 365)] public int LeadTimeDays { get; set; }

    [Required] public DateTime NextDueDate { get; set; }
}

/// <summary>
/// An edit may repoint the program at a different Specification version — that is the deliberate,
/// human act that hard version pinning requires when the document it schedules against is
/// revised. <see cref="MonitoringProgram.Status"/> is not editable here: Pause and Resume are
/// their own actions behind their own permission key.
/// </summary>
public class UpdateMonitoringProgramRequest : CreateMonitoringProgramRequest;

/// <summary>
/// What one run of the daily due-date scan did. Returned by the manual trigger and written to
/// the log by the hosted job, so "did the schedule actually raise anything last night" is
/// answerable.
/// </summary>
public class MonitoringScanResultDto
{
    public DateTime RanAt { get; set; }

    /// <summary>Programs that were Active and inside their lead time on this run.</summary>
    public int ProgramsDue { get; set; }

    /// <summary>Programs actually included in a generated round.</summary>
    public int ProgramsGenerated { get; set; }

    /// <summary>
    /// Rounds created. Strictly fewer than <see cref="ProgramsGenerated"/> whenever more than one
    /// due program shares a Type and Specification — which is the entire point of the grouping.
    /// </summary>
    public int TestRequestsCreated { get; set; }

    /// <summary>
    /// Due programs deliberately passed over, each with a stated reason: an open round already
    /// covers the point, or the Specification it is pinned to is no longer Effective. A skipped
    /// program's <see cref="MonitoringProgram.NextDueDate"/> is <b>not</b> advanced, so it stays
    /// visibly Overdue instead of disappearing from the schedule.
    /// </summary>
    public List<MonitoringScanSkipDto> Skipped { get; set; } = [];

    /// <summary>Active windows whose ValidUntil had passed and were moved to Expired on this run.</summary>
    public int WaterPeriodsExpired { get; set; }

    public List<Guid> CreatedTestRequestIds { get; set; } = [];
}

public class MonitoringScanSkipDto
{
    public Guid MonitoringProgramId { get; set; }
    public string SamplingPointCode { get; set; }
    public string Reason { get; set; }
}
