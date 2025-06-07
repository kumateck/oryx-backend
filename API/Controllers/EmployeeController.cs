using Microsoft.AspNetCore.Mvc;
using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.Employees;
using Microsoft.AspNetCore.Authorization;

namespace API.Controllers;
[Route("api/v{version:apiVersion}/employee")]
[ApiController]
[Authorize]
public class EmployeeController(IEmployeeRepository repository) : ControllerBase
{
    /// <summary>
    /// Sends emails to employees to fill in their details.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> OnboardEmployee([FromBody] OnboardEmployeeDto request)
    {
        var result = await repository.OnboardEmployees(request);
        return result.IsSuccess ? TypedResults.Ok() : result.ToProblemDetails();
    }

    /// <summary>
    /// Creates a new employee.
    /// </summary>
    [AllowAnonymous]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type= typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateEmployee([FromBody] CreateEmployeeRequest request)
    {
        var result = await repository.CreateEmployee(request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Creates a user from an employee.
    /// </summary>
    [HttpPost("user")]
    [ProducesResponseType(StatusCodes.Status200OK, Type= typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateUserFromEmployee([FromBody] EmployeeUserDto employeeUserDto)
    {
        var result = await repository.CreateEmployeeUser(employeeUserDto);
        return result.IsSuccess ? TypedResults.Ok() : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of employees based on search criteria.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<EmployeeDto>>))]
    public async Task<IResult> GetEmployees([FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null, [FromQuery] string designation = null, [FromQuery] string department = null)
    {
        var result = await repository.GetEmployees(page, pageSize, searchQuery, designation, department);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Retrieves a list of employees based on their department.
    /// </summary>
    [HttpGet("departments/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<EmployeeDto>))]
    public async Task<IResult> GetEmployeesByDepartment([FromRoute] Guid id)
    {
        
        var result = await repository.GetEmployeesByDepartment(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("available")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<MinimalEmployeeInfoDto>))]
    public async Task<IResult> GetAvailableEmployees([FromQuery] DateTime date)
    {
        var result = await repository.GetAvailableEmployees(date);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    /// <summary>
    /// Retrieves the details of a specific employee by its ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(EmployeeDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetEmployee([FromRoute] Guid id)
    {
        var result = await repository.GetEmployee(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    ///  Updates the details of an existing employee.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent, Type = typeof(EmployeeDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateEmployee([FromRoute] Guid id, [FromBody] CreateEmployeeRequest request)
    {
        var result = await repository.UpdateEmployee(id, request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates the department and designation of an existing employee.
    /// </summary>
    [HttpPut("{id:guid}/assign")]
    [ProducesResponseType(StatusCodes.Status204NoContent, Type = typeof(EmployeeDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> AssignEmployee([FromRoute] Guid id, AssignEmployeeDto employeeDto)
    {
        var result = await repository.AssignEmployee(id, employeeDto);
        return result.IsSuccess ? TypedResults.Ok() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a specific employee by its ID.
    /// </summary>
    [HttpDelete("{id:guid}")]
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
