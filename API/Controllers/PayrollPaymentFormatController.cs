using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.PayrollPaymentFormats;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;
[ApiController, Authorize]
[Route("api/v{version:apiVersion}/payroll/payment-formats")]
public class PayrollPaymentFormatController(IPayrollPaymentFormatRepository repository) : ControllerBase
{
    /// <summary>
    /// Creates a new payroll payment format.
    /// </summary>
    /// <param name="request">The CreatePayrollPaymentFormatRequest object.</param>
    /// <returns>Returns the ID of the created employee payroll payment format.</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreatePayrollPaymentFormat([FromBody] CreatePayrollPaymentFormatRequest request)
    {
        var result = await repository.CreatePayrollPaymentFormat(request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a payroll payment format by its ID.
    /// </summary>
    /// <param name="id">The ID of the payroll payment format.</param>
    /// <returns>Returns the payment format details.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PayrollPaymentFormatDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetPayrollPaymentFormat([FromRoute] Guid id)
    {
        var result = await repository.GetPayrollPaymentFormat(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of payroll payment formats.
    /// </summary>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="searchQuery">Search query for filtering results.</param>
    /// <param name="countryId">The country ID</param>
    /// <returns>Returns a paginated list of payroll payment formats.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<DepartmentDto>>))]
    public async Task<IResult> GetPayrollPaymentFormats([FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null, [FromQuery] Guid? countryId = null
            )
    {
        var result = await repository.GetPayrollPaymentFormats(page, pageSize, searchQuery, countryId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates a specific payroll payment format by its ID.
    /// </summary>
    /// <param name="request">The CreatePayrollPaymentFormatRequest object.</param>
    /// <param name="id">The ID of the payroll payment format.</param>
    /// <returns>Returns success or failure.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdatePayrollPaymentFormat([FromBody] CreatePayrollPaymentFormatRequest request, Guid id)
    {
        var result = await repository.UpdatePayrollPaymentFormat(id, request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a specific payroll payment format by its ID.
    /// </summary>
    /// <param name="id">The ID of the payment format to delete.</param>
    /// <returns>Returns success or failure.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeletePayrollPaymentFormat(Guid id)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.DeletePayrollPaymentFormat(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}