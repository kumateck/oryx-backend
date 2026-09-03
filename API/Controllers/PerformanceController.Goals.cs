using APP.Extensions;
using APP.Utils;
using DOMAIN.Entities.Performance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public partial class PerformanceController
{
    [HttpPost("goals")]
    [Authorize(PermissionKeys.CanCreateGoal)]
    public async Task<IResult> CreateGoal([FromBody] CreateGoalRequest request)
    {
        if (!TryGetUserId(out var userId)) return TypedResults.Unauthorized();

        var result = await repository.CreateGoal(request, userId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPut("goals/{id:guid}")]
    [Authorize(PermissionKeys.CanEditGoal)]
    public async Task<IResult> UpdateGoal([FromRoute] Guid id, [FromBody] UpdateGoalRequest request)
    {
        var result = await repository.UpdateGoal(id, request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    [HttpDelete("goals/{id:guid}")]
    [Authorize(PermissionKeys.CanDeleteGoal)]
    public async Task<IResult> DeleteGoal([FromRoute] Guid id)
    {
        if (!TryGetUserId(out var userId)) return TypedResults.Unauthorized();

        var result = await repository.DeleteGoal(id, userId);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    [HttpGet("goals")]
    [Authorize(PermissionKeys.CanViewOwnGoals)]
    public async Task<IResult> GetGoals(
        [FromQuery] Guid? employeeId, [FromQuery] Guid? cycleId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await repository.GetGoals(employeeId, cycleId, page, pageSize);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("goals/{id:guid}")]
    [Authorize(PermissionKeys.CanViewOwnGoals)]
    public async Task<IResult> GetGoal([FromRoute] Guid id)
    {
        var result = await repository.GetGoal(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}
