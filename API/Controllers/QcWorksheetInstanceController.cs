using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.QcWorksheets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// The Test Room: executing and reviewing a worksheet.
/// <para>
/// Start, enter, submit and review carry <b>paired</b> permission keys — one Chemical, one
/// Microbial — because chemical and microbial analysts are different people in different rooms.
/// Which key applies depends on the worksheet's own analysis track, not on the route, so those
/// actions resolve the track first and then check the matching key through the same
/// permission machinery the <c>[Authorize]</c> attribute uses.
/// </para>
/// <para>
/// Holding the key is never enough on its own: the repository additionally requires the caller
/// to be the worksheet's current assignee.
/// </para>
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/qc/worksheets/instances")]
[Authorize]
public class QcWorksheetInstanceController(
    IWorksheetInstanceRepository repository,
    IAuthorizationService authorizationService) : ControllerBase
{
    /// <summary>
    /// The full execution view: the computed header block, the pinned template version's
    /// sections and fields merged with recorded values, and resolved referenced results.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(QcWorksheetPermissionKeys.CanViewQcTestRequests)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WorksheetInstanceDetailDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetWorksheetInstance([FromRoute] Guid id)
    {
        var result = await repository.GetWorksheetInstance(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// "My Work" — only the worksheets assigned to the caller, which is what makes the Test
    /// Room a work queue rather than a record browser.
    /// </summary>
    [HttpGet("my-work")]
    [Authorize(QcWorksheetPermissionKeys.CanViewQcTestRequests)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WorksheetQueueDto))]
    public async Task<IResult> GetMyWork([FromQuery] SpecificationAnalysisType? analysisType = null)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.GetMyWork(Guid.Parse(userId), analysisType);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>"Awaiting My Review" — every submitted worksheet on the given track.</summary>
    [HttpGet("awaiting-review")]
    [Authorize(QcWorksheetPermissionKeys.CanViewQcTestRequests)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WorksheetQueueDto))]
    public async Task<IResult> GetReviewQueue([FromQuery] SpecificationAnalysisType? analysisType = null)
    {
        var result = await repository.GetReviewQueue(analysisType);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>Assigns a worksheet nobody has started yet.</summary>
    [HttpPost("{id:guid}/assign")]
    [Authorize(QcWorksheetPermissionKeys.CanAssignWorksheet)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WorksheetInstanceDetailDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> Assign(
        [FromRoute] Guid id, [FromBody] AssignWorksheetInstanceRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.Assign(id, request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Moves a worksheet to another analyst at any point before it is locked, recording a plain
    /// audit row. Values already entered keep their original attribution, and the worksheet's
    /// status does not change.
    /// </summary>
    [HttpPost("{id:guid}/reassign")]
    [Authorize(QcWorksheetPermissionKeys.CanReassignWorksheet)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WorksheetInstanceDetailDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> Reassign(
        [FromRoute] Guid id, [FromBody] ReassignWorksheetInstanceRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.Reassign(id, request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Starts a worksheet. The caller must be its assignee — rejected otherwise, even when they
    /// hold the permission key.
    /// </summary>
    [HttpPost("{id:guid}/start")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WorksheetInstanceDetailDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IResult> Start([FromRoute] Guid id)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var authorized = await AuthorizeForTrack(
            id,
            QcWorksheetPermissionKeys.CanStartChemicalWorksheet,
            QcWorksheetPermissionKeys.CanStartMicrobialWorksheet);

        if (authorized is not null) return authorized;

        var result = await repository.Start(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Records entered values, partially or in full. Runs the hard instrument and reagent gates
    /// before accepting anything, and does not change the worksheet's status.
    /// </summary>
    [HttpPut("{id:guid}/values")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WorksheetInstanceDetailDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IResult> SaveValues(
        [FromRoute] Guid id, [FromBody] SaveWorksheetValuesRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var authorized = await AuthorizeForTrack(
            id,
            QcWorksheetPermissionKeys.CanEnterChemicalWorksheetResult,
            QcWorksheetPermissionKeys.CanEnterMicrobialWorksheetResult);

        if (authorized is not null) return authorized;

        var result = await repository.SaveValues(id, request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Submits a completed worksheet for review, which puts it into the reviewers' pending
    /// queue through the existing approval engine.
    /// </summary>
    [HttpPost("{id:guid}/submit")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WorksheetInstanceDetailDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IResult> Submit([FromRoute] Guid id)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var authorized = await AuthorizeForTrack(
            id,
            QcWorksheetPermissionKeys.CanSubmitChemicalWorksheet,
            QcWorksheetPermissionKeys.CanSubmitMicrobialWorksheet);

        if (authorized is not null) return authorized;

        var result = await repository.Submit(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Reviews a submitted worksheet with an electronic signature: the caller's own password in
    /// addition to a valid session, then the same approval engine the generic endpoint calls.
    /// Declining returns the worksheet for correction, with the reason recorded on the signed
    /// approval round.
    /// </summary>
    [HttpPost("{id:guid}/review")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WorksheetInstanceDetailDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IResult> Review(
        [FromRoute] Guid id, [FromBody] ReviewWorksheetInstanceRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        var roleIds = (List<Guid>)HttpContext.Items["Roles"];
        if (userId is null) return TypedResults.Unauthorized();

        var authorized = await AuthorizeForTrack(
            id,
            QcWorksheetPermissionKeys.CanReviewChemicalWorksheet,
            QcWorksheetPermissionKeys.CanReviewMicrobialWorksheet);

        if (authorized is not null) return authorized;

        var result = await repository.Review(id, request, Guid.Parse(userId), roleIds ?? []);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Sends a submitted worksheet back to its analyst with a mandatory reason. The original
    /// assignee keeps it unless it is separately reassigned.
    /// </summary>
    [HttpPost("{id:guid}/return-for-correction")]
    [Authorize(QcWorksheetPermissionKeys.CanReturnWorksheetForCorrection)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WorksheetInstanceDetailDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> ReturnForCorrection(
        [FromRoute] Guid id, [FromBody] ReturnWorksheetForCorrectionRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.ReturnForCorrection(id, request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Checks the one of a paired Chemical/Microbial key that this worksheet's track calls for.
    /// Returns null when the caller is allowed through, or the failing result otherwise.
    /// </summary>
    private async Task<IResult> AuthorizeForTrack(Guid id, string chemicalKey, string microbialKey)
    {
        var track = await repository.GetAnalysisType(id);
        if (!track.IsSuccess) return track.ToProblemDetails();

        var key = track.Value == SpecificationAnalysisType.Chemical ? chemicalKey : microbialKey;
        var authorized = await authorizationService.AuthorizeAsync(User, key);

        return authorized.Succeeded ? null : TypedResults.Forbid();
    }
}
