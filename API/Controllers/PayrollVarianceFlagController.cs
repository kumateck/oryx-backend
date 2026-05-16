using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.PayrollVarianceFlags;
using DOMAIN.Entities.StatutoryProfiles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController, Authorize]
[Route("api/v{version:apiVersion}/payroll/runs/variance-flags")]
public class PayrollVarianceFlagController(IPayrollVarianceFlagRepository repository) : ControllerBase
{
    /// <summary>
    /// Retrieves a paginated list of statutory profiles.
    /// </summary>
    /// <param name="payrollRunId"></param>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="searchQuery">Search query for filtering results.</param>
    /// <param name="payrollVarianceSeverity"></param>
    /// <param name="payrollVarianceDisposition"></param>
    /// <returns>Returns a paginated list of statutory profiles.</returns>
    [HttpGet("{payrollRunId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<StatutoryProfileDto>>))]
    public async Task<IResult> GetStatutoryProfiles(Guid payrollRunId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null, [FromQuery] PayrollVarianceSeverity? payrollVarianceSeverity = null,
        [FromQuery] PayrollVarianceDisposition? payrollVarianceDisposition = null)
    {
        var result = await repository.GetVarianceFlags(payrollRunId, page, pageSize, searchQuery,
            payrollVarianceSeverity, payrollVarianceDisposition);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    
    /// <summary>
    /// Retrieves a specific variance flag by its ID.
    /// </summary>
    /// <param name="id">The ID of the variance flag.</param>
    /// <returns>Returns success or failure.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PayrollVarianceFlagDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetVarianceFlag([FromRoute] Guid id)
    {
        var result = await repository.GetVarianceFlag(id);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Creates a new statutory profile.
    /// </summary>
    /// <param name="payrollRunId"></param>
    /// <param name="request">The CreateStatutoryProfileRequest object.</param>
    /// <returns>Returns the ID of the created statutory profile.</returns>
    [HttpPost("{payrollRunId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> ReviewVarianceFlag([FromRoute] Guid payrollRunId, [FromBody] ReviewVarianceFlagRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();
        
        var result = await repository.ReviewVarianceFlag(payrollRunId,request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

}