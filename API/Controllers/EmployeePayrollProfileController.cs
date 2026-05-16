using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.EmployeePayrollProfiles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;


[Route("api/v{version:apiVersion}/payroll/employee-profiles")]
[ApiController]
[Authorize]
public class EmployeePayrollProfileController(IEmployeePayrollProfileRepository repository) : ControllerBase
{
    /// <summary>
    /// Creates a new employee payroll profile.
    /// </summary>
    /// <param name="request">The CreateEmployeePayrollProfileRequest object.</param>
    /// <returns>Returns the ID of the created employee payroll profile.</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateEmployeePayrollProfile([FromBody] CreateEmployeePayrollProfileRequest request)
    {
        var result = await repository.CreateEmployeePayrollProfile(request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves an employee payroll profile by its ID.
    /// </summary>
    /// <param name="employeeProfileId">The ID of the employee payroll profile.</param>
    /// <returns>Returns the employee profile details.</returns>
    [HttpGet("{employeeProfileId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(EmployeePayrollProfileDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetEmployeePayrollProfile([FromRoute] Guid employeeProfileId)
    {
        var result = await repository.GetEmployeePayrollProfile(employeeProfileId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of employee payroll profiles.
    /// </summary>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="searchQuery">Search query for filtering results.</param>
    /// <param name="payGroupId"></param>
    /// <returns>Returns a paginated list of departments.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<EmployeePayrollProfileDto>>))]
    public async Task<IResult> GetEmployeePayrollProfiles([FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null, [FromQuery] Guid? payGroupId = null)
    {
        var result = await repository.GetEmployeePayrollProfiles(page, pageSize, searchQuery, payGroupId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves an employee payroll profile by the employee id.
    /// </summary>
    /// <param name="employeeId"></param>
    /// <returns>Returns a paginated list of departments.</returns>
    [HttpGet("employee/{employeeId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(EmployeePayrollProfileDto))]
    public async Task<IResult> GetEmployeePayrollProfileByEmployee([FromRoute] Guid employeeId)
    {
        var result = await repository.GetEmployeePayrollProfileByEmployee(employeeId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates a specific department by its ID.
    /// </summary>
    /// <param name="request">The CreateEmployeePayrollProfileRequest object.</param>
    /// <param name="employeeProfileId">The ID of the employee profile.</param>
    /// <returns>Returns success or failure.</returns>
    [HttpPut("{employeeProfileId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateDepartment([FromBody] CreateEmployeePayrollProfileRequest request, [FromRoute] Guid employeeProfileId)
    {
        var result = await repository.UpdateEmployeePayrollProfile(employeeProfileId, request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a specific department by its ID.
    /// </summary>
    /// <param name="employeeProfileId">The ID of the employee profile to delete.</param>
    /// <returns>Returns success or failure.</returns>
    [HttpDelete("{employeeProfileId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteEmployeePayrollProfile([FromRoute] Guid employeeProfileId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.DeleteEmployeePayrollProfile(employeeProfileId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}