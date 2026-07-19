using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.PayrollPaymentBatches;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;
[ApiController]
[Route("api/v{version:apiVersion}/payroll/runs/{payrollRunId:guid}/payment-batch")]
public class PayrollPaymentBatchController(IPayrollPaymentBatchRepository repository) : ControllerBase
{
    /// <summary>
    /// Creates a new payroll payment batch.
    /// </summary>
    /// <param name="payrollRunId">The payroll run Id.</param>
    /// <returns>Returns the ID of the created payroll payment batch.</returns>
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreatePayrollPaymentBatch([FromRoute] Guid payrollRunId)
    {
        var result = await repository.CreatePayrollPaymentBatch(payrollRunId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a payroll payment batch by its ID.
    /// </summary>
    /// <param name="id">The ID of the payroll payment batch.</param>
    /// <param name="payrollRunId">The payroll run ID</param>
    /// <returns>Returns the payroll payment batch details.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PayrollPaymentBatchDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetPayrollPaymentBatch([FromRoute] Guid id, Guid payrollRunId)
    {
        var result = await repository.GetPaymentBatch(id,  payrollRunId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of payroll payment batches.
    /// </summary>
    /// <param name="payrollRunId"></param>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="searchQuery">Search query for filtering results.</param>
    /// <param name="status"></param>
    /// <returns>Returns a paginated list of payroll elements.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<DepartmentDto>>))]
    public async Task<IResult> GetPayrollPaymentBatches([FromQuery] Guid payrollRunId,[FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null, 
        [FromQuery] PayrollPaymentBatchStatus? status = null
            )
    {
        var result = await repository.GetPaymentBatches(page, pageSize, searchQuery, payrollRunId, status);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Releases a specific payroll payment batch by its ID.
    /// </summary>
    /// <param name="request">The CreatePayrollElementRequest object.</param>
    /// <param name="id">The ID of the payroll element.</param>
    /// <param name="payrollRunId"></param>
    /// <returns>Returns success or failure.</returns>
    [HttpPut("{id:guid}/release")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> ReleasePaymentBatch([FromBody] ReleasePaymentBatchRequest request, [FromRoute] Guid id, [FromRoute] Guid payrollRunId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.ReleasePaymentBatch(id, payrollRunId, request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Cancels a specific payroll payment batch by its ID.
    /// </summary>
    /// <param name="id">The ID of the payroll payment batch.</param>
    /// <param name="payrollRunId"></param>
    /// <returns>Returns success or failure.</returns>
    [HttpPut("{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> CancelPaymentBatch([FromRoute] Guid id, [FromRoute] Guid payrollRunId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.CancelPaymentBatch(id, payrollRunId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}