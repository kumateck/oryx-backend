using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.PayrollElements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;
[ApiController, Authorize]
[Route("api/v{version:apiVersion}/payroll/elements")]
public class PayrollElementController(IPayrollElementRepository repository) : ControllerBase
{
    /// <summary>
    /// Creates a new payroll element.
    /// </summary>
    /// <param name="request">The CreatePayrollElementRequest object.</param>
    /// <returns>Returns the ID of the created employee payroll element.</returns>
    [HttpPost("")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreatePayrollElement([FromBody] CreatePayrollElementRequest request)
    {
        var result = await repository.CreatePayrollElement(request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a payroll element by its ID.
    /// </summary>
    /// <param name="id">The ID of the payroll element.</param>
    /// <returns>Returns the payroll element details.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PayrollElementDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetPayrollElement([FromRoute] Guid id)
    {
        var result = await repository.GetPayrollElement(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of payroll elements.
    /// </summary>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="searchQuery">Search query for filtering results.</param>
    /// <param name="employeeId"></param>
    /// <param name="payrollCompanyId"></param>
    /// <param name="payrollElementType"></param>
    /// <returns>Returns a paginated list of payroll elements.</returns>
    [HttpGet("")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<DepartmentDto>>))]
    public async Task<IResult> GetPayrollElements([FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null, [FromQuery] Guid? employeeId = null, [FromQuery] Guid? payrollCompanyId = null,
        [FromQuery] PayrollElementType? payrollElementType = null
            )
    {
        var result = await repository.GetPayrollElements(page, pageSize, searchQuery, payrollCompanyId, payrollElementType);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates a specific payroll element by its ID.
    /// </summary>
    /// <param name="request">The CreatePayrollElementRequest object.</param>
    /// <param name="id">The ID of the payroll element.</param>
    /// <returns>Returns success or failure.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdatePayrollElement([FromBody] CreatePayrollElementRequest request, [FromRoute] Guid id)
    {
        var result = await repository.UpdatePayrollElement(id, request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a specific payroll element by its ID.
    /// </summary>
    /// <param name="id">The ID of the element to delete.</param>
    /// <returns>Returns success or failure.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeletePayrollElement(Guid id)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.DeletePayrollElement(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
    
    #region Payroll Element Versions

    /// <summary>
    /// Creates a new payroll element version.
    /// </summary>
    /// <param name="request">The CreatePayrollElementVersionRequest object.</param>
    /// <param name="id"></param>
    /// <returns>Returns the ID of the created employee payroll element version.</returns>
    [HttpPost("{id:guid}/versions")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreatePayrollElementVersion([FromBody] CreatePayrollElementVersionRequest request, [FromRoute] Guid id)
    {
        var result = await repository.CreatePayrollElementVersion(request, id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a payroll element version by its ID.
    /// </summary>
    /// <param name="id">The ID of the payroll element.</param>
    /// <returns>Returns the payroll element details.</returns>
    [HttpGet("{id:guid}/versions")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PayrollElementVersionDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetPayrollElementVersion([FromRoute] Guid id)
    {
        var result = await repository.GetPayrollElementVersion(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates a specific payroll element version by its ID.
    /// </summary>
    /// <param name="request">The CreatePayrollElementVersionRequest object.</param>
    /// <param name="id">The ID of the payroll element.</param>
    /// <returns>Returns success or failure.</returns>
    [HttpPut("{id:guid}/versions")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdatePayrollElementVersion([FromBody] CreatePayrollElementVersionRequest request, [FromRoute] Guid id)
    {
        var result = await repository.UpdatePayrollElementVersion(id, request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
    
    #endregion
}