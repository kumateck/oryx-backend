using APP.Extensions;
using APP.Services.Materials;
using APP.Utils;
using DOMAIN.Entities.Materials.Batch;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SHARED;

namespace API.Controllers;

[ApiController]
[Authorize]
[Route("api/v{version:apiVersion}/qc/material-retests")]
public sealed class MaterialRetestController(IMaterialRetestService retests) : ControllerBase
{
    [HttpGet]
    [Authorize(QcWorksheetPermissionKeys.CanViewMaterialRetests)]
    public async Task<IResult> List([FromQuery] Guid? batchId)
    {
        var result = await retests.ListAsync(batchId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("{id:guid}")]
    [Authorize(QcWorksheetPermissionKeys.CanViewMaterialRetestDetail)]
    public async Task<IResult> Get(Guid id)
    {
        var result = await retests.GetAsync(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("batches/{batchId:guid}")]
    [Authorize(QcWorksheetPermissionKeys.CanRequestMaterialRetest)]
    public async Task<IResult> CreateRequest(Guid batchId, [FromBody] CreateMaterialRetestRequest body)
    {
        if (!Actor(out var actor)) return TypedResults.Unauthorized();
        var result = await retests.RequestAsync(batchId, body?.Reason, actor, false);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(QcWorksheetPermissionKeys.CanApproveMaterialRetest)]
    public async Task<IResult> Approve(Guid id, [FromBody] MaterialRetestDecision body)
        => await Decide(id, body, true);

    [HttpPost("{id:guid}/reject")]
    [Authorize(QcWorksheetPermissionKeys.CanRejectMaterialRetest)]
    public async Task<IResult> Reject(Guid id, [FromBody] MaterialRetestDecision body)
        => await Decide(id, body, false);

    [HttpPost("{id:guid}/sample")]
    [Authorize(QcWorksheetPermissionKeys.CanRecordMaterialRetestSample)]
    public async Task<IResult> Sample(Guid id, [FromBody] RecordMaterialRetestSample body)
    {
        if (!Actor(out var actor)) return TypedResults.Unauthorized();
        var result = await retests.RecordSampleAsync(id, body, actor);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    [HttpPost("{id:guid}/acknowledge")]
    [Authorize(QcWorksheetPermissionKeys.CanAcknowledgeMaterialRetest)]
    public async Task<IResult> Acknowledge(Guid id)
    {
        if (!Actor(out var actor)) return TypedResults.Unauthorized();
        var result = await retests.AcknowledgeAsync(id, actor);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    [HttpPost("{id:guid}/test-request")]
    [Authorize(QcWorksheetPermissionKeys.CanLinkMaterialRetestTestRequest)]
    public async Task<IResult> LinkTestRequest(Guid id, [FromBody] LinkMaterialRetestTestRequest body)
    {
        if (!Actor(out var actor)) return TypedResults.Unauthorized();
        var result = await retests.LinkTestRequestAsync(id, body.TestRequestId, actor);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private async Task<IResult> Decide(Guid id, MaterialRetestDecision body, bool approve)
    {
        if (!Actor(out var actor)) return TypedResults.Unauthorized();
        var roles = HttpContext.Items["Roles"] as List<Guid> ?? [];
        var result = await retests.DecideAsync(id, actor, roles, body, approve);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private bool Actor(out Guid actor)
    {
        actor = Guid.Empty;
        return Guid.TryParse(HttpContext.Items["Sub"] as string, out actor);
    }
}
