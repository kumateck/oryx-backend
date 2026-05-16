using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.PayrollLoanLedgers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController, Authorize]
[Route("api/v{version:apiVersion}/payroll-loan-ledger")]
public class PayrollLoanLedgerController(IPayrollLoanLedgerRepository repository) : ControllerBase
{

    /// <summary>
    /// Retrieves a payroll loan ledger by its ID.
    /// </summary>
    /// <param name="id">The ID of the payroll element.</param>
    /// <returns>Returns the payroll element details.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PayrollLoanLedgerDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetPayrollLoanLedgerEntry([FromRoute] Guid id)
    {
        var result = await repository.GetLoanLedgerEntry(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of payroll elements.
    /// </summary>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="searchQuery">Search query for filtering results.</param>
    /// <param name="employeeId"></param>
    /// <param name="payrollElementAssignmentId"></param>
    /// <returns>Returns a paginated list of payroll elements.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<DepartmentDto>>))]
    public async Task<IResult> GetPayrollLoanLedgerEntries([FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null, [FromQuery] Guid? employeeId = null, [FromQuery] Guid? payrollElementAssignmentId = null
            )
    {
        var result = await repository.GetLoanLedgerEntries(page, pageSize, searchQuery, employeeId, payrollElementAssignmentId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
}