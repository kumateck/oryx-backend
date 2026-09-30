using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.QcWorksheets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// Water validity windows and the uses booked against them.
/// <para>
/// There is deliberately <b>no create endpoint</b>. A window appears at
/// <c>PendingActivation</c> when a Water certificate is issued, and covers nothing at all until
/// somebody activates it. That gap is the point: a window's validity is backdated to the sample's
/// collection time, so activating one tells production it may rely on water it has already been
/// consuming throughout incubation — a claim a person makes, with a reason, not one the system
/// makes on their behalf.
/// </para>
/// <para>
/// Entirely additive. Nothing here reads or writes the live water path this module coexists with.
/// </para>
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/qc/worksheets/water-quality")]
[Authorize]
public class QcWaterQualityController(IQcWaterQualityRepository repository) : ControllerBase
{
    /// <summary>
    /// Lists water validity windows, filterable by status and sampling point. Unpaginated: the
    /// register is read to answer "what covers this point right now", and a partial page could
    /// hide the answer.
    /// </summary>
    [HttpGet("periods")]
    [Authorize(QcWorksheetPermissionKeys.CanViewQcWaterQualityPeriods)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<WaterQualityPeriodSummaryDto>))]
    public async Task<IResult> GetPeriods(
        [FromQuery] WaterQualityPeriodStatus? status = null,
        [FromQuery] Guid? samplingPointId = null)
    {
        var result = await repository.GetPeriods(status, samplingPointId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// One window with its full use log. Held records stay in the log and are distinguishable by
    /// their own status — a flagged use is never removed, since what it flags is exactly what a
    /// Quality Impact Assessment needs to see.
    /// </summary>
    [HttpGet("periods/{id:guid}")]
    [Authorize(QcWorksheetPermissionKeys.CanViewQcWaterQualityPeriods)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WaterQualityPeriodDetailDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetPeriod([FromRoute] Guid id)
    {
        var result = await repository.GetPeriod(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// PendingActivation to Active.
    /// <para>
    /// Requires a retrospective reason, and sets <c>ValidUntil</c> to the sampling point's next
    /// scheduled test as it stands at this moment — dynamic, not a fixed offset from
    /// <c>ValidFrom</c>. A point with no active monitoring program has no next test to bound the
    /// window against, and is refused rather than given an open-ended window.
    /// </para>
    /// </summary>
    [HttpPost("periods/{id:guid}/activate")]
    [Authorize(QcWorksheetPermissionKeys.CanActivateQcWaterQualityPeriod)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WaterQualityPeriodDetailDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> Activate(
        [FromRoute] Guid id, [FromBody] ActivateWaterQualityPeriodRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.Activate(id, request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Active to Held, cascading to every water use recorded under the window — each moves to
    /// Held, which is the Quality Impact Assessment flag. The QIA investigation itself is a QA
    /// process and is deliberately not modelled here.
    /// </summary>
    [HttpPost("periods/{id:guid}/hold")]
    [Authorize(QcWorksheetPermissionKeys.CanHoldQcWaterQualityPeriod)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WaterQualityPeriodDetailDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> Hold(
        [FromRoute] Guid id, [FromBody] HoldWaterQualityPeriodRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.Hold(id, request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Records one water use against an Active window.
    /// <para>
    /// Manual entry. Automatic capture from production consumption was explicitly deferred and
    /// stays deferred — nothing in the production path calls this.
    /// </para>
    /// <para>
    /// Refused against a PendingActivation or Held window: booking a use against a window that
    /// covers nothing would assert coverage that was never granted.
    /// </para>
    /// </summary>
    [HttpPost("uses")]
    [Authorize(QcWorksheetPermissionKeys.CanRecordQcWaterUse)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WaterUseRecordDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> RecordUse([FromBody] RecordWaterUseRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.RecordUse(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}
