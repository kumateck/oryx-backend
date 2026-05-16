using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.PayrollPostingEvents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController, Authorize]
[Route("api/v{version:apiVersion}/payroll/runs/posting-events")]
public class PayrollPostingEventController(IPayrollPostingEventRepository repository) : ControllerBase
{

    /// <summary>
    /// Retrieves a posting event by its ID.
    /// </summary>
    /// <param name="id">The ID of the statutory profile.</param>
    /// <returns>Returns the statutory profile details.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PayrollPostingEventDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetStatutoryProfile([FromRoute] Guid id)
    {
        var result = await repository.GetPostingEvent(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of posting events.
    /// </summary>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="searchQuery">Search query for filtering results.</param>
    /// <param name="payrollRunId">The payroll run ID</param>
    /// <param name="status"></param>
    /// <returns>Returns a paginated list of posting events.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<PayrollPostingEventDto>>))]
    public async Task<IResult> GetPostingEvents([FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null, [FromQuery] Guid? payrollRunId = null,
        [FromQuery] PayrollPostingEventStatus? status = null)
    {
        var result = await repository.GetPostingEvents(page, pageSize, searchQuery, payrollRunId, status);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
  
}