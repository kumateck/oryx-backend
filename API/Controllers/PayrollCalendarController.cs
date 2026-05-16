using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.PayrollCalendars;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;
[Route("api/v{version:apiVersion}/payroll/calendars")]
[ApiController]
[Authorize]
public class PayrollCalendarController(IPayrollCalenderRepository repository) : ControllerBase
{
    /// <summary>
    /// Creates a new payroll calendar.
    /// </summary>
    /// <param name="request">The create request object.</param>
    /// <returns>Returns the ID of the created record.</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreatePayrollCalendar(
        [FromBody] CreatePayrollCalendarRequest request)
    {
        var result = await repository.CreatePayrollCalendar(request);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a specific payrollCalendar by ID.
    /// </summary>
    /// <param name="payrollCalendarId">The ID of the record.</param>
    /// <returns>Returns the entity details.</returns>
    [HttpGet("{payrollCalendarId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PayrollCalendarDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetPayrollCalendar(
        [FromRoute] Guid payrollCalendarId)
    {
        var result = await repository.GetPayrollCalendar(payrollCalendarId);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of payroll calendars.
    /// </summary>
    /// <param name="page">Current page number.</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <param name="searchQuery">Optional search query.</param>
    /// <returns>Returns paginated results.</returns>
    [HttpGet]
    [ProducesResponseType(
        StatusCodes.Status200OK,
        Type = typeof(Paginateable<IEnumerable<PayrollCalendarDto>>)
    )]
    public async Task<IResult> GetPayrollCalendars(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null)
    {
        var result = await repository.GetPayrollCalendars(
            page,
            pageSize,
            searchQuery);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates a specific payroll calendar.
    /// </summary>
    /// <param name="request">The update request object.</param>
    /// <param name="payrollCalendarId">The ID of the entity.</param>
    /// <returns>Returns success or failure.</returns>
    [HttpPut("{payrollCalendarId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdatePayrollCalendar(
        [FromBody] CreatePayrollCalendarRequest request,
        [FromRoute] Guid payrollCalendarId)
    {
        var result = await repository.UpdatePayrollCalendar(
            payrollCalendarId,
            request);

        return result.IsSuccess
            ? TypedResults.NoContent()
            : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a specific payroll calendar.
    /// </summary>
    /// <param name="payrollCalendarId">The ID of the entity to delete.</param>
    /// <returns>Returns success or failure.</returns>
    [HttpDelete("{payrollCalendarId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeletePayrollCalendar(
        [FromRoute] Guid payrollCalendarId)
    {
        var userId = (string)HttpContext.Items["Sub"];

        if (userId is null)
            return TypedResults.Unauthorized();

        var result = await repository.DeletePayrollCalendar(
            payrollCalendarId,
            Guid.Parse(userId));

        return result.IsSuccess
            ? TypedResults.NoContent()
            : result.ToProblemDetails();
    }
}
