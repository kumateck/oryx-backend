using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.PayrollValidationIssues;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public class PayrollValidationIssueController(IPayrollValidationIssueRepository repository) : ControllerBase
{

    /// <summary>
    /// Retrieves a statutory profile by its ID.
    /// </summary>
    /// <param name="id">The ID of the statutory profile.</param>
    /// <returns>Returns the statutory profile details.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PayrollValidationIssueDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetValidationIssue([FromRoute] Guid id)
    {
        var result = await repository.GetValidationIssue(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of payroll validation issues.
    /// </summary>
    /// <param name="payrollRunId"></param>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="searchQuery">Search query for filtering results.</param>
    /// <param name="payrollValidationStage"></param>
    /// <param name="payrollValidationSeverity"></param>
    /// <param name="payrollValidationIssueStatus"></param>
    /// <returns>Returns a paginated list of statutory profiles.</returns>
    [HttpGet("{payrollRunId:guid}/issues")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<PayrollValidationIssueDto>>))]
    public async Task<IResult> GetValidationIssues([FromRoute] Guid payrollRunId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null, [FromQuery] PayrollValidationStage? payrollValidationStage = null,
        [FromQuery] PayrollValidationSeverity? payrollValidationSeverity = null,
        [FromQuery] PayrollValidationIssueStatus? payrollValidationIssueStatus = null)
    {
        var result = await repository.GetValidationIssues(payrollRunId, page, pageSize, searchQuery,
            payrollValidationStage, payrollValidationSeverity, payrollValidationIssueStatus);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Resolves a payroll validation issue.
    /// </summary>
    /// <param name="request">The ResolveValidationIssueRequest object.</param>
    /// <param name="id">The ID of the payroll run.</param>
    /// <returns>Returns success or failure.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> ResolveValidationIssue([FromBody] ResolveValidationIssueRequest request, [FromRoute] Guid id)
    {
        var userId = (string) HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.ResolveValidationIssue(id, request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
    
}