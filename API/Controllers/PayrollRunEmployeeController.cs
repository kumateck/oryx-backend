using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.StatutoryProfiles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController, Authorize]
[Route("api/v{version:apiVersion}/payroll-run-employee")]
public class PayrollRunEmployeeController(IPayrollRunEmployeeRepository repository) : ControllerBase
{

    /// <summary>
    /// Retrieves a statutory profile by its ID.
    /// </summary>
    /// <param name="id">The ID of the statutory profile.</param>
    /// <returns>Returns the statutory profile details.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(StatutoryProfileDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetStatutoryProfile([FromRoute] Guid id)
    {
        var result = await repository.GetRunEmployee(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of statutory profiles.
    /// </summary>
    /// <param name="payrollRunId"></param>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="searchQuery">Search query for filtering results.</param>
    /// <param name="isExcluded"></param>
    /// <returns>Returns a paginated list of statutory profiles.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<StatutoryProfileDto>>))]
    public async Task<IResult> GetPayrollRunEmployees([FromQuery] Guid payrollRunId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null, [FromQuery] bool? isExcluded = null)
    {
        var result = await repository.GetRunEmployees(payrollRunId,page, pageSize, searchQuery, isExcluded);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

}