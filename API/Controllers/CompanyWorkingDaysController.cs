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
        
        var result = await repository.CreateCompanyWorkingDays(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(): result.ToProblemDetails();
    }

    /// <summary>
    /// Returns a paginated list of company working days based on search criteria.
    /// </summary>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<CompanyWorkingDaysDto>>))]
    public async Task<IResult> GetWorkingDays([FromQuery] int page, [FromQuery] int pageSize, [FromQuery] string searchQuery)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.GetCompanyWorkingDays(page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Retrieves the details of a specific company working day by its ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CompanyWorkingDaysDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> GetWorkingDaysByCompanyId([FromRoute] Guid id)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.GetCompanyWorkingDay(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates the details of an existing company working day.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CompanyWorkingDaysDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateWorkingDays([FromRoute] Guid id, [FromBody] CompanyWorkingDaysRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.UpdateCompanyWorkingDays(id, request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok() : result.ToProblemDetails();
    }
}