using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.ShiftAssignments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/shift-category")]
[Authorize]
public class ShiftCategoryController(IShiftCategoryRepository repository) : ControllerBase
{
    /// <summary>
    /// Creates a new shift category.
    /// </summary>
    [HttpPost]
    [Authorize(PermissionKeys.CanCreateShiftCategory)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateShiftCategory([FromBody] CreateShiftCategoryRequest request)
    {
        var result = await repository.CreateShiftCategory(request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of shift categories.
    /// </summary>
    [HttpGet]
    [Authorize(PermissionKeys.CanViewShiftCategories)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<ShiftCategoryDto>>))]
    public async Task<IResult> GetShiftCategories(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchQuery = null)
    {
        var result = await repository.GetShiftCategories(page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a specific shift category by its ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(PermissionKeys.CanViewShiftCategories)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ShiftCategoryDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetShiftCategory([FromRoute] Guid id)
    {
        var result = await repository.GetShiftCategory(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates an existing shift category.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(PermissionKeys.CanEditShiftCategory)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateShiftCategory([FromRoute] Guid id, [FromBody] CreateShiftCategoryRequest request)
    {
        var result = await repository.UpdateShiftCategory(id, request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a specific shift category by its ID.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(PermissionKeys.CanDeleteShiftCategory)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteShiftCategory([FromRoute] Guid id)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.DeleteShiftCategory(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}
