using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.PayrollRuns;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController, Authorize]
[Route("api/v{version:apiVersion}/payroll/runs")]
public class PayrollRunController(IPayrollRunRepository repository) : ControllerBase
{ 
    /// <summary>
    /// Creates a new payroll run.
    /// </summary>
    /// <param name="request">The CreatePayrollRunRequest object.</param>
    /// <returns>Returns the ID of the created payroll run.</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreatePayrollRun([FromBody] CreatePayrollRunRequest request)
    {
        var result = await repository.CreatePayrollRun(request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a payroll run by its ID.
    /// </summary>
    /// <param name="id">The ID of the payroll run.</param>
    /// <returns>Returns the payroll run details.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PayrollRunDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetPayrollRun([FromRoute] Guid id)
    {
        var result = await repository.GetPayrollRun(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of payroll runs.
    /// </summary>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="searchQuery">Search query for filtering results.</param>
    /// <param name="countryId">The country ID</param>
    /// <returns>Returns a paginated list of payroll runs.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<PayrollRunDto>>))]
    public async Task<IResult> GetPayrollRuns([FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null, [FromQuery] Guid? countryId = null)
    {
        var result = await repository.GetPayrollRuns(page, pageSize, searchQuery, countryId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates a specific payroll run by its ID.
    /// </summary>
    /// <param name="request">The CreatePayrollRunRequest object.</param>
    /// <param name="id">The ID of the payroll run.</param>
    /// <returns>Returns success or failure.</returns>
    [HttpPut("{id:guid}/transition")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> TransitionPayrollRun([FromBody] PayrollRunTransitionRequest request, [FromRoute] Guid id)
    {
        var userId = (string) HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.TransitionPayrollRun(id, request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Cancels a specific payroll run by its ID.
    /// </summary>
    /// <param name="request">The CreatePayrollRunRequest object.</param>
    /// <param name="id">The ID of the payroll run.</param>
    /// <returns>Returns success or failure.</returns>
    [HttpPut("{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> CancelPayrollRun([FromBody] CancelPayrollRunRequest request, [FromRoute] Guid id)
    {
        var userId = (string) HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.CancelPayrollRun(id, request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
    /// <summary>
    /// Deletes a specific payroll run by its ID.
    /// </summary>
    /// <param name="id">The ID of the payroll run.</param>
    /// <returns>Returns success or failure.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeletePayrollRun([FromRoute] Guid id)
    {
        var userId = (string) HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.DeletePayrollRun(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}