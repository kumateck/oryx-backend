using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.PayrollRetroAdjustments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController, Authorize]
[Route("api/v{version:apiVersion}/payroll/retro-adjustments")]
public class PayrollRetroAdjustmentController(IPayrollRetroAdjustmentRepository repository) : ControllerBase
{
    /// <summary>
    /// Creates a new payroll retro adjustment.
    /// </summary>
    /// <param name="request">The CreatePayrollRetroAdjustmentRequest object.</param>
    /// <returns>Returns the ID of the created payroll retro adjustment.</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreatePayrollRetroAdjustment([FromBody] CreatePayrollRetroAdjustmentRequest request)
    {
        var result = await repository.CreatePayrollRetroAdjustment(request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a payroll retro adjustment by its ID.
    /// </summary>
    /// <param name="id">The ID of the payroll retro adjustment.</param>
    /// <returns>Returns the payroll retro adjustment details.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PayrollRetroAdjustmentDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetPayrollRetroAdjustment([FromRoute] Guid id)
    {
        var result = await repository.GetRetroAdjustment(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of payroll retro adjustments.
    /// </summary>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="searchQuery">Search query for filtering results.</param>
    /// <param name="payrollCompanyId">The country ID</param>
    /// <param name="employeeId"></param>
    /// <param name="adjustmentStatus"></param>
    /// <returns>Returns a paginated list of payroll retro adjustments.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<PayrollRetroAdjustmentDto>>))]
    public async Task<IResult> GetPayrollRetroAdjustments([FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null, [FromQuery] Guid? payrollCompanyId = null,
        [FromQuery] Guid? employeeId = null, [FromQuery] PayrollRetroAdjustmentStatus? adjustmentStatus = null)
    {
        var result = await repository.GetRetroAdjustments(page, pageSize, searchQuery, payrollCompanyId, employeeId, adjustmentStatus);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Materializes a specific payroll retro adjustment by its ID.
    /// </summary>
    /// <param name="request">The CreatePayrollRetroAdjustmentRequest object.</param>
    /// <param name="id">The ID of the payroll retro adjustment.</param>
    /// <returns>Returns success or failure.</returns>
    [HttpPut("{id:guid}/materialize")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> MaterializeRetroAdjustment([FromBody] MaterializeRetroAdjustmentRequest request, [FromRoute] Guid id)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.MaterializeRetroAdjustment(id, request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
    
    
    /// <summary>
    /// Deletes a specific payroll retro adjustment by its ID.
    /// </summary>
    /// <param name="id">The ID of the payroll retro adjustment.</param>
    /// <returns>Returns success or failure.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeletePayrollRetroAdjustment([FromRoute] Guid id)
    {
        var userId = (string) HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.DeleteRetroAdjustment(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}