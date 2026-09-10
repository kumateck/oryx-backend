using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.RndStabilityStudies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/rnd/stability-chambers")]
[Authorize]
public class RndStabilityChamberController(IRndStabilityStudyRepository repository) : ControllerBase
{
    /// <summary>
    /// Creates a new stability chamber.
    /// </summary>
    [HttpPost]
    [Authorize(PermissionKeys.CanManageRndStabilityChambers)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    public async Task<IResult> CreateChamber([FromBody] CreateRndStabilityChamberRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.CreateChamber(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of stability chambers.
    /// </summary>
    [HttpGet]
    [Authorize(PermissionKeys.CanViewRndStabilityStudies)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<RndStabilityChamberDto>>))]
    public async Task<IResult> GetChambers([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await repository.GetChambers(page, pageSize);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}

[ApiController]
[Route("api/v{version:apiVersion}/rnd/projects/{rndProjectId:guid}/stability-studies")]
[Authorize]
public class RndStabilityStudyController(IRndStabilityStudyRepository repository) : ControllerBase
{
    /// <summary>
    /// Creates a new stability study for an R&amp;D trial batch, generating its pull-point schedule.
    /// </summary>
    [HttpPost]
    [Authorize(PermissionKeys.CanCreateRndStabilityStudy)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateStudy(
        [FromRoute] Guid rndProjectId,
        [FromBody] CreateRndStabilityStudyRequest request
    )
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.CreateStudy(rndProjectId, request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Moves a stability study to Completed or Terminated.
    /// </summary>
    [HttpPut("{studyId:guid}/status")]
    [Authorize(PermissionKeys.CanEditRndStabilityStudy)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateStatus(
        [FromRoute] Guid studyId,
        [FromBody] UpdateRndStabilityStudyStatusRequest request
    )
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.UpdateStudyStatus(studyId, request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Records the result for a pull point, marking it Reported.
    /// </summary>
    [HttpPut("pull-points/{pullPointId:guid}/result")]
    [Authorize(PermissionKeys.CanEditRndStabilityStudy)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> RecordPullPointResult(
        [FromRoute] Guid pullPointId,
        [FromBody] RecordPullPointResultRequest request
    )
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.RecordPullPointResult(pullPointId, request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a stability study by its ID, including its pull-point schedule.
    /// </summary>
    [HttpGet("{studyId:guid}")]
    [Authorize(PermissionKeys.CanViewRndStabilityStudies)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(RndStabilityStudyDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetStudy([FromRoute] Guid studyId)
    {
        var result = await repository.GetStudy(studyId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves the paginated list of stability studies for an R&amp;D project.
    /// </summary>
    [HttpGet]
    [Authorize(PermissionKeys.CanViewRndStabilityStudies)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<RndStabilityStudyDto>>))]
    public async Task<IResult> GetStudiesForProject(
        [FromRoute] Guid rndProjectId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10
    )
    {
        var result = await repository.GetStudiesForProject(rndProjectId, page, pageSize);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}
