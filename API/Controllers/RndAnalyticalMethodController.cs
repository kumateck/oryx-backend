using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.RndAnalyticalMethods;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/rnd/projects/{rndProjectId:guid}/analytical-methods")]
[Authorize]
public class RndAnalyticalMethodController(IRndAnalyticalMethodRepository repository) : ControllerBase
{
    /// <summary>
    /// Creates a new analytical method-in-development for an R&amp;D project.
    /// </summary>
    [HttpPost]
    [Authorize(PermissionKeys.CanCreateRndAnalyticalMethod)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateMethod(
        [FromRoute] Guid rndProjectId,
        [FromBody] CreateRndAnalyticalMethodRequest request
    )
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.CreateMethod(rndProjectId, request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates an analytical method's details while it is still Draft.
    /// </summary>
    [HttpPut("{methodId:guid}")]
    [Authorize(PermissionKeys.CanEditRndAnalyticalMethod)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateMethod(
        [FromRoute] Guid methodId,
        [FromBody] CreateRndAnalyticalMethodRequest request
    )
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.UpdateMethod(methodId, request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Moves an analytical method to the next status (Draft to UnderValidation to Validated).
    /// </summary>
    [HttpPut("{methodId:guid}/status")]
    [Authorize(PermissionKeys.CanEditRndAnalyticalMethod)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateStatus(
        [FromRoute] Guid methodId,
        [FromBody] UpdateRndAnalyticalMethodStatusRequest request
    )
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.UpdateStatus(methodId, request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>Transfers a validated method into a traceable production STP.</summary>
    [HttpPost("{methodId:guid}/transfer")]
    [Authorize(PermissionKeys.CanTransferRndAnalyticalMethod)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> TransferToStp(
        [FromRoute] Guid methodId,
        [FromBody] TransferRndAnalyticalMethodRequest request
    )
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.TransferToStp(methodId, request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves an analytical method by its ID.
    /// </summary>
    [HttpGet("{methodId:guid}")]
    [Authorize(PermissionKeys.CanViewRndAnalyticalMethods)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(RndAnalyticalMethodDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetMethod([FromRoute] Guid methodId)
    {
        var result = await repository.GetMethod(methodId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves the paginated list of analytical methods for an R&amp;D project.
    /// </summary>
    [HttpGet]
    [Authorize(PermissionKeys.CanViewRndAnalyticalMethods)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<RndAnalyticalMethodDto>>))]
    public async Task<IResult> GetMethodsForProject(
        [FromRoute] Guid rndProjectId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10
    )
    {
        var result = await repository.GetMethodsForProject(rndProjectId, page, pageSize);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}
