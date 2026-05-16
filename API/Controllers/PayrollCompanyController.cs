using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.PayrollCompanies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/payroll/companies")]
[Authorize]
public class PayrollCompanyController(IPayrollCompanyRepository repository) : ControllerBase
{

    /// <summary>
    /// Creates a payroll company.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreatePayrollCompany([FromBody] CreatePayrollCompanyRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.CreatePayrollCompany(request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();

    }

    /// <summary>
    /// Returns a paginated list of payroll companies.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<PayrollCompanyDto>>))]
    public async Task<IResult> GetPayrollCompanies([FromQuery] int page = 1,
        [FromQuery] int pageSize = 10, [FromQuery] string searchQuery = null,
        [FromQuery] Guid? designationId = null)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.GetPayrollCompanies(page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();

    }

    /// <summary>
    /// Retrieves the details of a specific payroll company.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PayrollCompanyDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetPayrollCompany([FromRoute] Guid id)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.GetPayrollCompany(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();

    }

    /// <summary>
    /// Updates the details of an existing payroll company.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent, Type = typeof(PayrollCompanyDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdatePayrollCompany([FromRoute] Guid id, [FromBody] CreatePayrollCompanyRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.UpdatePayrollCompany(id, request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();

    }

    /// <summary>
    /// Deletes a specific payroll company by its ID.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeletePayrollCompany([FromRoute] Guid id)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.DeletePayrollCompany(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}