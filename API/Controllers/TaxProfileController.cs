using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.TaxProfiles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController, Authorize]
[Route("api/v{version:apiVersion}/tax-profiles")]
public class TaxProfileController(ITaxProfileRepository repository) : ControllerBase
{
    /// <summary>
    /// Creates a new tax profile.
    /// </summary>
    /// <param name="request">The CreateTaxProfileRequest object.</param>
    /// <returns>Returns the ID of the created tax profile.</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateTaxProfile([FromBody] CreateTaxProfileRequest request)
    {
        var result = await repository.CreateTaxProfile(request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a tax profile by its ID.
    /// </summary>
    /// <param name="id">The ID of the tax profile.</param>
    /// <returns>Returns the tax profile details.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TaxProfileDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetTaxProfile([FromRoute] Guid id)
    {
        var result = await repository.GetTaxProfile(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of tax profiles.
    /// </summary>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="searchQuery">Search query for filtering results.</param>
    /// <param name="countryId">The country ID</param>
    /// <returns>Returns a paginated list of tax profiles.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<TaxProfileDto>>))]
    public async Task<IResult> GetTaxProfiles([FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null, [FromQuery] Guid? countryId = null)
    {
        var result = await repository.GetTaxProfiles(page, pageSize, searchQuery, countryId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates a specific tax profile by its ID.
    /// </summary>
    /// <param name="request">The CreateTaxProfileRequest object.</param>
    /// <param name="id">The ID of the tax profile.</param>
    /// <returns>Returns success or failure.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateTaxProfile([FromBody] CreateTaxProfileRequest request, [FromRoute] Guid id)
    {
        var result = await repository.UpdateTaxProfile(id, request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Deletes a specific tax profile by its ID.
    /// </summary>
    /// <param name="id">The ID of the tax profile.</param>
    /// <returns>Returns success or failure.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteTaxProfile([FromRoute] Guid id)
    {
        var userId = (string) HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.DeleteTaxProfile(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}