using APP.Extensions;
using APP.Utils;
using DOMAIN.Entities.Performance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public partial class PerformanceController
{
    [HttpPost("cycles")]
    [Authorize(PermissionKeys.CanManagePerformanceCycles)]
    public async Task<IResult> CreateCycle([FromBody] CreatePerformanceCycleRequest request)
    {
        if (!TryGetUserId(out var userId)) return TypedResults.Unauthorized();

        var result = await repository.CreateCycle(request, userId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("cycles")]
    [Authorize(PermissionKeys.CanViewPerformanceCycles)]
    public async Task<IResult> GetCycles()
    {
        var result = await repository.GetCycles();
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("cycles/{id:guid}")]
    [Authorize(PermissionKeys.CanViewPerformanceCycles)]
    public async Task<IResult> GetCycle([FromRoute] Guid id)
    {
        var result = await repository.GetCycle(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPut("cycles/{id:guid}/status")]
    [Authorize(PermissionKeys.CanManagePerformanceCycles)]
    public async Task<IResult> UpdateCycleStatus([FromRoute] Guid id, [FromBody] UpdatePerformanceCycleStatusRequest request)
    {
        var result = await repository.UpdateCycleStatus(id, request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}
