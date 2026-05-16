using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.PayrollElementAssignments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;
[ApiController]
[Route("api/v{version:apiVersion}/payroll/element-assignments")]
[Authorize]
public class PayrollElementAssignmentController(IPayrollElementAssignmentRepository repository) : ControllerBase
{
    /// <summary>
    /// Creates a new payroll element assignment.
    /// </summary>
    /// <param name="request">The CreatePayrollElementAssignmentRequest object.</param>
    /// <returns>Returns the ID of the created employee payroll element assignment.</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreatePayrollElementAssignment([FromBody] CreatePayrollElementAssignment request)
    {
        var result = await repository.CreatePayrollElementAssignment(request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves an employee payroll profile by its ID.
    /// </summary>
    /// <param name="id">The ID of the payroll element assignment.</param>
    /// <returns>Returns the employee profile details.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PayrollElementAssignmentDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetPayrollElementAssignment([FromRoute] Guid id)
    {
        var result = await repository.GetPayrollElementAssignment(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of payroll element assignments.
    /// </summary>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="searchQuery">Search query for filtering results.</param>
    /// <param name="employeeId"></param>
    /// <param name="payrollElementId"></param>
    /// <param name="payrollElementStatus"></param>
    /// <returns>Returns a paginated list of payroll element assignments.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<PayrollElementAssignmentDto>>))]
    public async Task<IResult> GetPayrollElementAssignments([FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null, [FromQuery] Guid? employeeId = null, [FromQuery] Guid? payrollElementId = null,
        [FromQuery] PayrollElementAssignmentStatus? payrollElementStatus = null
            )
    {
        var result = await repository.GetPayrollElementAssignments(page, pageSize, searchQuery, employeeId, status: payrollElementStatus);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates a specific payroll element assignment by its ID.
    /// </summary>
    /// <param name="request">The CreatePayrollElementAssignmentRequest object.</param>
    /// <param name="id">The ID of the payroll element assignment.</param>
    /// <returns>Returns success or failure.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdatePayrollElementAssignment([FromBody] CreatePayrollElementAssignment request, Guid id)
    {
        var result = await repository.UpdatePayrollElementAssignment(id, request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a specific payroll element assignment by its ID.
    /// </summary>
    /// <param name="id">The ID of the element assignment to delete.</param>
    /// <returns>Returns success or failure.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeletePayrollElementAssignment(Guid id)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.DeletePayrollElementAssignment(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}