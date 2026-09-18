using APP.Extensions;
using APP.IRepository;
using APP.Services.QcWorksheets;
using APP.Utils;
using DOMAIN.Entities.QcWorksheets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// Monitoring programs: the per-sampling-point schedule behind routine Water and Environmental
/// testing, plus the sampling point master data the schedule is built on.
/// <para>
/// Plain CRUD with Pause/Resume and <b>no approval lifecycle</b>: a monitoring program is
/// operational configuration, not a controlled document. How often a tap is sampled is not a
/// thing anybody signs for — the Specification it is sampled against is, and that has its own
/// full lifecycle in Milestone 2.
/// </para>
/// <para>
/// Sampling points sit under this same controller and behind these same keys. The build brief for
/// this milestone states its permission key list is already defined and adds none, and a point
/// exists only to be scheduled: creating one and creating the program that schedules it are the
/// same act of configuration by the same person.
/// </para>
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/qc/worksheets/monitoring-programs")]
[Authorize]
public class QcMonitoringProgramController(
    IMonitoringProgramRepository repository,
    ISamplingPointRepository samplingPoints,
    IQcMonitoringScanService scanService) : ControllerBase
{
    // -----------------------------------------------------------------------
    // Sampling points (master data)
    // -----------------------------------------------------------------------

    /// <summary>
    /// Lists sampling points, optionally narrowed to one discipline. Deliberately unpaginated:
    /// this populates the point picker on a program form and on a routine subject, and a partial
    /// page would hide points the author needs.
    /// </summary>
    [HttpGet("sampling-points")]
    [Authorize(QcWorksheetPermissionKeys.CanViewMonitoringPrograms)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<SamplingPointDto>))]
    public async Task<IResult> GetSamplingPoints(
        [FromQuery] string searchQuery = null, [FromQuery] SamplingPointType? type = null)
    {
        var result = await samplingPoints.GetSamplingPoints(searchQuery, type);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>Retrieves a single sampling point.</summary>
    [HttpGet("sampling-points/{id:guid}")]
    [Authorize(QcWorksheetPermissionKeys.CanViewMonitoringPrograms)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SamplingPointDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetSamplingPoint([FromRoute] Guid id)
    {
        var result = await samplingPoints.GetSamplingPoint(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>Creates a sampling point. Codes are unique among live points.</summary>
    [HttpPost("sampling-points")]
    [Authorize(QcWorksheetPermissionKeys.CanCreateMonitoringProgram)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SamplingPointDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> CreateSamplingPoint([FromBody] CreateSamplingPointRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await samplingPoints.CreateSamplingPoint(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates a sampling point. Its Type is refused once a program, a round or a water quality
    /// period already resolves against it.
    /// </summary>
    [HttpPut("sampling-points/{id:guid}")]
    [Authorize(QcWorksheetPermissionKeys.CanEditMonitoringProgram)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SamplingPointDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateSamplingPoint(
        [FromRoute] Guid id, [FromBody] UpdateSamplingPointRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await samplingPoints.UpdateSamplingPoint(id, request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>Deletes a sampling point. Refused while any monitoring program still schedules it.</summary>
    [HttpDelete("sampling-points/{id:guid}")]
    [Authorize(QcWorksheetPermissionKeys.CanEditMonitoringProgram)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteSamplingPoint([FromRoute] Guid id)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await samplingPoints.DeleteSamplingPoint(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    // -----------------------------------------------------------------------
    // Monitoring programs
    // -----------------------------------------------------------------------

    /// <summary>
    /// Lists monitoring programs ordered by due date, each carrying the calendar bucket it falls
    /// in — Overdue, Due Today, Due This Week, Scheduled or Paused. Unpaginated on purpose: the
    /// calendar view exists to surface overdue points, which pagination would bury.
    /// </summary>
    [HttpGet]
    [Authorize(QcWorksheetPermissionKeys.CanViewMonitoringPrograms)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<MonitoringProgramDto>))]
    public async Task<IResult> GetMonitoringPrograms(
        [FromQuery] string searchQuery = null,
        [FromQuery] MonitoringProgramStatus? status = null,
        [FromQuery] SamplingPointType? samplingPointType = null,
        [FromQuery] Guid? samplingPointId = null)
    {
        var result = await repository.GetMonitoringPrograms(
            searchQuery, status, samplingPointType, samplingPointId);

        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>Retrieves a single monitoring program.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(QcWorksheetPermissionKeys.CanViewMonitoringPrograms)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MonitoringProgramDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetMonitoringProgram([FromRoute] Guid id)
    {
        var result = await repository.GetMonitoringProgram(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Creates a monitoring program, pinned to the exact Specification version named — which must
    /// be Effective and must apply to the sampling point's own discipline.
    /// </summary>
    [HttpPost]
    [Authorize(QcWorksheetPermissionKeys.CanCreateMonitoringProgram)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MonitoringProgramDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> CreateMonitoringProgram([FromBody] CreateMonitoringProgramRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.CreateMonitoringProgram(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates a monitoring program, including repointing it at a newer Specification version —
    /// the deliberate act hard version pinning requires when the document is revised, since
    /// nothing in this module resolves a Specification forward on its own.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(QcWorksheetPermissionKeys.CanEditMonitoringProgram)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MonitoringProgramDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateMonitoringProgram(
        [FromRoute] Guid id, [FromBody] UpdateMonitoringProgramRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.UpdateMonitoringProgram(id, request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>Pauses a schedule. The daily scan skips paused programs entirely.</summary>
    [HttpPost("{id:guid}/pause")]
    [Authorize(QcWorksheetPermissionKeys.CanPauseMonitoringProgram)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MonitoringProgramDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> Pause([FromRoute] Guid id)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.Pause(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Resumes a paused schedule, leaving its due date where it was — a program paused past
    /// several occurrences comes back overdue rather than silently rolled forward.
    /// </summary>
    [HttpPost("{id:guid}/resume")]
    [Authorize(QcWorksheetPermissionKeys.CanPauseMonitoringProgram)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MonitoringProgramDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> Resume([FromRoute] Guid id)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.Resume(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Runs the daily due-date scan on demand, as of now.
    /// <para>
    /// The scan normally runs unattended once a day; this is the manual trigger for the morning
    /// after a failed pass, and it is idempotent for the same reason the job is — every program
    /// included in a generated round has its due date advanced in the same transaction, so a
    /// second run finds nothing left due.
    /// </para>
    /// <para>
    /// Behind <see cref="QcWorksheetPermissionKeys.CanCreateScheduledQcTestRequest"/>, not a
    /// monitoring-program key: what this actually does is raise scheduled test requests, and that
    /// key exists precisely for the manual override of a system-raised round.
    /// </para>
    /// </summary>
    [HttpPost("scan")]
    [Authorize(QcWorksheetPermissionKeys.CanCreateScheduledQcTestRequest)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MonitoringScanResultDto))]
    public async Task<IResult> RunScan()
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await scanService.RunAsync(DateTime.UtcNow, Guid.Parse(userId));
        return TypedResults.Ok(result);
    }
}
