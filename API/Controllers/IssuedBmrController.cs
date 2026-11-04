using APP.Extensions;
using APP.Repository;
using APP.Utils;
using DOMAIN.Entities.Products.Production;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController, Route("api/v{version:apiVersion}/issued-bmrs"), Tags("ProductionSchedule")]
[Authorize(PermissionKeys.CanViewIssuedBmrRecords)]
public class IssuedBmrController(IssuedBmrRepository repository) : ControllerBase
{
    private Guid? Actor() => Guid.TryParse(HttpContext.Items["Sub"] as string, out var actor) ? actor : null;

    [HttpGet(Name = "ListIssuedBmrRecords")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IssuedBmrPage))]
    public async Task<IResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 50, [FromQuery] string searchQuery = null)
    {
        if (Actor() is not { } actor) return TypedResults.Unauthorized();
        var result = await repository.List(actor, page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("{id:guid}/history", Name = "GetIssuedBmrHistory")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IssuedBmrHistory))]
    public async Task<IResult> History(Guid id)
    {
        if (Actor() is not { } actor) return TypedResults.Unauthorized();
        var result = await repository.History(id, actor);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPut("{id:guid}/batch-number", Name = "CorrectIssuedBmrBatchNumber")]
    [Authorize(PermissionKeys.CanCorrectIssuedBmrBatchNumber)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IResult> CorrectNumber(Guid id, [FromBody] CorrectIssuedBmrNumber request)
    {
        if (Actor() is not { } actor) return TypedResults.Unauthorized();
        var result = await repository.CorrectNumber(id, request, actor);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    [HttpPost("{id:guid}/revisions", Name = "RequestIssuedBmrRevision")]
    [Authorize(PermissionKeys.CanRequestIssuedBmrRevision)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    public async Task<IResult> RequestRevision(Guid id, [FromBody] RequestIssuedBmrRevision request)
    {
        if (Actor() is not { } actor) return TypedResults.Unauthorized();
        var result = await repository.RequestRevision(id, request, actor);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("{id:guid}/revisions/{revisionId:guid}/review", Name = "ReviewIssuedBmrRevision")]
    [Authorize(PermissionKeys.CanApproveIssuedBmrRevision)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IResult> ReviewRevision(Guid id, Guid revisionId, [FromBody] ReviewIssuedBmrRevision request)
    {
        if (Actor() is not { } actor) return TypedResults.Unauthorized();
        var result = await repository.ReviewRevision(id, revisionId, request, actor);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}
