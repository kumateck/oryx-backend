using Microsoft.AspNetCore.Mvc;
using APP.Extensions;
using APP.IRepository;
using DOMAIN.Entities.Employees;
using Microsoft.AspNetCore.Authorization;

namespace API.Controllers;
[Route("api/v{version:apiVersion}/employee")]
[ApiController]
public class EmployeeController(IEmployeeRepository repository) : ControllerBase
{
    [HttpPost("register")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> OnboardEmployee([FromBody] OnboardEmployeeDto request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.OnboardEmployees(request);
        return result.IsSuccess ? TypedResults.Ok() : result.ToProblemDetails();
    }

    [HttpGet("{id:Guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(EmployeeDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetEmployee(Guid id)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.GetEmployee(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPut("{id:Guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateEmployee(Guid id, [FromBody] CreateEmployeeRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.UpdateEmployee(id, request,Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}
