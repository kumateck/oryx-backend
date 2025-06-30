using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.CompanyWorkingDays;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/working-days")]
public class CompanyWorkingDaysController(ICompanyWorkingDaysRepository repository) : ControllerBase
{

    /// <summary>
    /// Creates the company working days.
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateWorkingDays([FromBody] List<CompanyWorkingDaysRequest> request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.CreateCompanyWorkingDays(request);
        return result.IsSuccess ? TypedResults.Ok(): result.ToProblemDetails();
    }

    /// <summary>
    /// Returns a paginated list of company working days based on search criteria.
    /// </summary>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<CompanyWorkingDaysDto>>))]
    public async Task<IResult> GetWorkingDays([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchQuery = null)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.GetCompanyWorkingDays(page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
}