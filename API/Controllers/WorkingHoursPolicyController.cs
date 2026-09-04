using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.ShiftAssignments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/working-hours-policy")]
[Authorize]
public class WorkingHoursPolicyController(IWorkingHoursPolicyRepository repository) : ControllerBase
{
    /// <summary>
    /// Creates a new effective-dated working hours policy.
    /// </summary>
    [HttpPost]
    [Authorize(PermissionKeys.CanManageWorkingHoursPolicy)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreatePolicy([FromBody] CreateWorkingHoursPolicyRequest request)
    {
        var result = await repository.CreatePolicy(request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Lists all working hours policies, most recent first.
    /// </summary>
    [HttpGet]
    [Authorize(PermissionKeys.CanViewWorkingHoursPolicy)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<WorkingHoursPolicyDto>))]
    public async Task<IResult> GetPolicies()
    {
        var result = await repository.GetPolicies();
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a working hours policy.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(PermissionKeys.CanManageWorkingHoursPolicy)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeletePolicy([FromRoute] Guid id)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.DeletePolicy(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}
