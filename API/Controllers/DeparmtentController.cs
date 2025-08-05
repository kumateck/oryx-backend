using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Departments.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SHARED;

namespace API.Controllers;

[Route("api/v{version:apiVersion}/department")]
[ApiController]
public class DepartmentController(IDepartmentRepository repository) : ControllerBase
{
    /// <summary>
    /// Creates a new department.
    /// </summary>
    /// <param name="request">The CreateDepartmentRequest object.</param>
    /// <returns>Returns the ID of the created department.</returns>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateDepartment([FromBody] CreateDepartmentRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.CreateDepartment(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a department by its ID.
    /// </summary>
    /// <param name="departmentId">The ID of the department.</param>
    /// <returns>Returns the department details.</returns>
    [HttpGet("{departmentId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DepartmentDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetDepartment(Guid departmentId)
    {
        var result = await repository.GetDepartment(departmentId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of departments.
    /// </summary>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="searchQuery">Search query for filtering results.</param>
    /// <param name="type">The type of the department. (Product or Non Product Department)</param>
    /// <returns>Returns a paginated list of departments.</returns>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<DepartmentDto>>))]
    public async Task<IResult> GetDepartments([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchQuery = null, [FromQuery] DepartmentType? type = null)
    {
        var result = await repository.GetDepartments(page, pageSize, searchQuery, type);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates a specific department by its ID.
    /// </summary>
    /// <param name="request">The CreateDepartmentRequest object.</param>
    /// <param name="departmentId">The ID of the department to update.</param>
    /// <returns>Returns success or failure.</returns>
    [HttpPut("{departmentId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateDepartment([FromBody] CreateDepartmentRequest request, Guid departmentId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.UpdateDepartment(request, departmentId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a specific department by its ID.
    /// </summary>
    /// <param name="departmentId">The ID of the department to delete.</param>
    /// <returns>Returns success or failure.</returns>
    [HttpDelete("{departmentId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteDepartment(Guid departmentId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.DeleteDepartment(departmentId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}
