using APP.Extensions;
using APP.Services.QcWorksheets;
using APP.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SHARED;

namespace API.Controllers;

[ApiController]
[Authorize]
[Route("api/v{version:apiVersion}/qc/rework-tasks")]
public sealed class QcResponseReworkController(IResponseReworkService rework) : ControllerBase
{
    [HttpGet]
    [Authorize(QcWorksheetPermissionKeys.CanViewResponseReworkTasks)]
    public async Task<IResult> List()
    {
        var result = await rework.ListAsync();
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("{id:guid}")]
    [Authorize(QcWorksheetPermissionKeys.CanViewResponseReworkTaskDetail)]
    public async Task<IResult> Get(Guid id)
    {
        var result = await rework.GetAsync(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("{id:guid}/start")]
    [Authorize(QcWorksheetPermissionKeys.CanStartResponseReworkTask)]
    public async Task<IResult> Start(Guid id)
    {
        if (!Guid.TryParse(HttpContext.Items["Sub"] as string, out var actorId))
            return TypedResults.Unauthorized();
        var result = await rework.StartAsync(id, actorId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}
