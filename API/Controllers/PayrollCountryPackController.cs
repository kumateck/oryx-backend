using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.PayrollCountryPack;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;
[ApiController]
[Route("api/v{version:apiVersion}/payroll/country-packs")]
[Authorize]
public class PayrollCountryPackController(IPayrollCountryPackRepository repository) : ControllerBase
{
    /// <summary>
    /// Creates a new payroll country pack.
    /// </summary>
    /// <param name="request">The CreatePayrollCountryPackRequest object.</param>
    /// <returns>Returns the ID of the created country pack.</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateEmployeePayrollProfile([FromBody] CreatePayrollCountryPackRequest request)
    {
        var result = await repository.CreatePayrollCountryPack(request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a country pack by its ID.
    /// </summary>
    /// <param name="packId">The ID of the country pack.</param>
    /// <returns>Returns the  profile details.</returns>
    [HttpGet("{packId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PayrollCountryPackDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetPayrollCountryPack([FromRoute] Guid packId)
    {
        var result = await repository.GetPayrollCountryPack(packId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of payroll country packs.
    /// </summary>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="searchQuery">Search query for filtering results.</param>
    /// <param name="countryId">The country Id</param>
    /// <returns>Returns a paginated list of departments.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<PayrollCountryPackDto>>))]
    public async Task<IResult> GetPayrollCountryPacks([FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null, [FromQuery] Guid? countryId = null)
    {
        var result = await repository.GetPayrollCountryPacks(page, pageSize, searchQuery, countryId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    

    /// <summary>
    /// Updates a specific country pack by its ID.
    /// </summary>
    /// <param name="request">The CreatePayrollCountryPackRequest object.</param>
    /// <param name="packId">The ID of the pack.</param>
    /// <returns>Returns success or failure.</returns>
    [HttpPut("{packId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateDepartment([FromBody] CreatePayrollCountryPackRequest request, [FromRoute] Guid packId)
    {
        var result = await repository.UpdatePayrollCountryPack(packId, request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a specific country pack by its ID.
    /// </summary>
    /// <param name="packId">The ID of the  profile to delete.</param>
    /// <returns>Returns success or failure.</returns>
    [HttpDelete("{packId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteEmployeePayrollProfile([FromRoute] Guid packId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.DeletePayrollCountryPack(packId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}
