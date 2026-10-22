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
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    public async Task<IResult> CreateGoal([FromBody] CreateGoalRequest request)
    {
        if (!TryGetUserId(out var userId)) return TypedResults.Unauthorized();
        if (!await MayActForEmployee(request.EmployeeId)) return TypedResults.Forbid();
        var result = await repository.CreateGoal(request, userId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPut("goals/{id:guid}")]
    [Authorize(PermissionKeys.CanEditGoal)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IResult> UpdateGoal([FromRoute] Guid id, [FromBody] UpdateGoalRequest request)
    {
        if (await repository.GoalOwner(id) is not { } owner) return TypedResults.NotFound();
        if (!await MayActForEmployee(owner)) return TypedResults.Forbid();
        var result = await repository.UpdateGoal(id, request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    [HttpDelete("goals/{id:guid}")]
    [Authorize(PermissionKeys.CanDeleteGoal)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IResult> DeleteGoal([FromRoute] Guid id)
    {
        if (!TryGetUserId(out var userId)) return TypedResults.Unauthorized();
        if (await repository.GoalOwner(id) is not { } owner) return TypedResults.NotFound();
        if (!await MayActForEmployee(owner)) return TypedResults.Forbid();
        var result = await repository.DeleteGoal(id, userId);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Goals, newest target first. Without team access only the signed-in employee's own goals are returned, or, for a
    /// review's assigned reviewer, the reviewed employee's goals in that review's cycle.
    /// </summary>
    [HttpGet("goals")]
    [Authorize(PermissionKeys.CanViewOwnGoals)]
    public async Task<IResult> GetGoals(
        [FromQuery] Guid? employeeId, [FromQuery] Guid? cycleId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (!TryGetUserId(out var userId)) return TypedResults.Unauthorized();
        if (!await Has(PermissionKeys.CanViewTeamGoals))
        {
            if (await repository.EmployeeOf(userId) is not { } own) return TypedResults.Forbid();
            // A reviewer may read the goals they are rating: that employee's, for the cycle of a review assigned to them.
            var reviewing = employeeId.HasValue && employeeId != own && cycleId.HasValue && await repository.Reviews(own, employeeId.Value, cycleId.Value);
            if (employeeId.HasValue && employeeId != own && !reviewing) return TypedResults.Forbid();
            if (!reviewing) employeeId = own;
        }
        var result = await repository.GetGoals(employeeId, cycleId, page, pageSize);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("goals/{id:guid}")]
    [Authorize(PermissionKeys.CanViewOwnGoals)]
    public async Task<IResult> GetGoal([FromRoute] Guid id)
    {
        if (await repository.GoalOwner(id) is not { } owner) return TypedResults.NotFound();
        if (!await MayActForEmployee(owner)) return TypedResults.Forbid();
        var result = await repository.GetGoal(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}
