using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.PayRollPeriods;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController, Authorize]
[Route("api/v{version:apiVersion}/payroll/periods")]
public class PayrollPeriodController(IPayrollPeriodRepository repository) : ControllerBase
{
    /// <summary>
    /// Creates a new payroll period.
    /// </summary>
    /// <param name="request">The CreatePayrollPeriodRequest object.</param>
    /// <returns>Returns the ID of the created payroll period.</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreatePayrollPeriod([FromBody] CreatePayrollPeriodRequest request)
    {
        var result = await repository.CreatePayrollPeriod(request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a payroll period by its ID.
    /// </summary>
    /// <param name="id">The ID of the payroll period.</param>
    /// <returns>Returns the payroll period details.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PayrollPeriodDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetPayrollPeriod([FromRoute] Guid id)
    {
        var result = await repository.GetPayrollPeriod(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of payroll periods.
    /// </summary>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="searchQuery">Search query for filtering results.</param>
    /// <param name="countryId">The country ID</param>
    /// <param name="periodStatus"></param>
    /// <returns>Returns a paginated list of payroll periods.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<PayrollPeriodDto>>))]
    public async Task<IResult> GetPayrollPeriods([FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null, [FromQuery] Guid? countryId = null, 
        [FromQuery] PayrollPeriodStatus? periodStatus = null)
    {
        var result = await repository.GetPayrollPeriods(page, pageSize, searchQuery, countryId, periodStatus);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates a specific payroll period by its ID.
    /// </summary>
    /// <param name="request">The CreatePayrollPeriodRequest object.</param>
    /// <param name="id">The ID of the payroll period.</param>
    /// <returns>Returns success or failure.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdatePayrollPeriod([FromBody] CreatePayrollPeriodRequest request, [FromRoute] Guid id)
    {
        var result = await repository.UpdatePayrollPeriod(id, request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Reopens a specific payroll period by its ID.
    /// </summary>
    /// <param name="request">The CreatePayrollPeriodRequest object.</param>
    /// <param name="id">The ID of the payroll period.</param>
    /// <returns>Returns success or failure.</returns>
    [HttpPut("{id:guid}/reopen")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> ReopenPayrollPeriod([FromBody] CreatePayrollPeriodRequest request, [FromRoute] Guid id)
    {
        var result = await repository.UpdatePayrollPeriod(id, request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Hard closes or soft closes a specific payroll period by its ID.
    /// </summary>
    /// <param name="request">The CreatePayrollPeriodRequest object.</param>
    /// <returns>Returns success or failure.</returns>
    [HttpPut("close")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> ClosePayrollPeriod([FromBody] ClosePayrollPeriodRequest request)
    {
        var userId = (string) HttpContext.Items["Sub"];
        var result = await repository.ClosePayrollPeriod(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Deletes a specific payroll period by its ID.
    /// </summary>
    /// <param name="id">The ID of the payroll period.</param>
    /// <returns>Returns success or failure.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeletePayrollPeriod([FromRoute] Guid id)
    {
        var userId = (string) HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.DeletePayrollPeriod(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}