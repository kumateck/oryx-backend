using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.StatutoryProfiles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController, Authorize]
[Route("api/v{version:apiVersion}/statutory-profiles")]
public class StatutoryProfileController(IStatutoryProfileRepository repository) : ControllerBase
{
    /// <summary>
    /// Creates a new statutory profile.
    /// </summary>
    /// <param name="request">The CreateStatutoryProfileRequest object.</param>
    /// <returns>Returns the ID of the created statutory profile.</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateStatutoryProfile([FromBody] CreateStatutoryProfileRequest request)
    {
        var result = await repository.CreateStatutoryProfile(request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

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
        var result = await repository.GetStatutoryProfile(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of statutory profiles.
    /// </summary>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="searchQuery">Search query for filtering results.</param>
    /// <param name="countryId">The country ID</param>
    /// <returns>Returns a paginated list of statutory profiles.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<StatutoryProfileDto>>))]
    public async Task<IResult> GetStatutoryProfiles([FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null, [FromQuery] Guid? countryId = null)
    {
        var result = await repository.GetStatutoryProfiles(page, pageSize, searchQuery, countryId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates a specific statutory profile by its ID.
    /// </summary>
    /// <param name="request">The CreateStatutoryProfileRequest object.</param>
    /// <param name="id">The ID of the statutory profile.</param>
    /// <returns>Returns success or failure.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateStatutoryProfile([FromBody] CreateStatutoryProfileRequest request, [FromRoute] Guid id)
    {
        var result = await repository.UpdateStatutoryProfile(id, request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Deletes a specific statutory profile by its ID.
    /// </summary>
    /// <param name="id">The ID of the statutory profile.</param>
    /// <returns>Returns success or failure.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteStatutoryProfile([FromRoute] Guid id)
    {
        var userId = (string) HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.DeleteStatutoryProfile(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}