using Microsoft.AspNetCore.Mvc;
using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.Employees;
using Microsoft.AspNetCore.Authorization;

namespace API.Controllers;
[Route("api/v{version:apiVersion}/employee")]
[ApiController]
public class EmployeeController(IEmployeeRepository repository) : ControllerBase
{
    /// <summary>
    /// Sends emails to employees to fill in their details.
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> OnboardEmployee([FromBody] OnboardEmployeeDto request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.OnboardEmployees(request);
        return result.IsSuccess ? TypedResults.Ok() : result.ToProblemDetails();
    }

    /// <summary>
    /// Creates a new employee.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK, Type= typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateEmployee([FromBody] CreateEmployeeRequest request)
    {
        var userId = (string) HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.CreateEmployee(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Creates a user from an employee.
    /// </summary>
    [HttpPost("user")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type= typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateUserFromEmployee([FromBody] EmployeeUserDto employeeUserDto)
    {
        var userId = (string) HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.CreateEmployeeUser(employeeUserDto, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok() : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of employees based on search criteria.
    /// </summary>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<EmployeeDto>>))]
    public async Task<IResult> GetEmployees([FromQuery] int page, [FromQuery] int pageSize,
        [FromQuery] string searchQuery, [FromQuery] string designation, [FromQuery] string department)
    {
        var userId = (string) HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.GetEmployees(page, pageSize, searchQuery, designation, department);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves the details of a specific employee by its ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(EmployeeDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetEmployee([FromRoute] Guid id)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.GetEmployee(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    ///  Updates the details of an existing employee.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent, Type = typeof(EmployeeDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateEmployee([FromRoute] Guid id, [FromBody] CreateEmployeeRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.UpdateEmployee(id, request,Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates the department and designation of an existing employee.
    /// </summary>
    [HttpPut("{id:guid}/assign")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent, Type = typeof(EmployeeDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> AssignEmployee([FromRoute] Guid id, AssignEmployeeDto employeeDto)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.AssignEmployee(id, employeeDto, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a specific employee by its ID.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteEmployee([FromRoute] Guid id)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.DeleteEmployee(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}
