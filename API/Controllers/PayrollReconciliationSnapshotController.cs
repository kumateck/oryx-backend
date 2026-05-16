using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.PayrollReconciliationSnapshot;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController, Authorize]
[Route("api/v{version:apiVersion}/payroll/reports/reconciliations")]
public class PayrollReconciliationSnapshotController(IPayrollReconciliationSnapshotRepository repository) : ControllerBase
{

    /// <summary>
    /// Retrieves a reconciliation snapshot by its ID.
    /// </summary>
    /// <param name="id">The ID of the reconciliation snapshot.</param>
    /// <returns>Returns the reconciliation snapshot details.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PayrollReconciliationSnapshotDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetReconciliationSnapshot([FromRoute] Guid id)
    {
        var result = await repository.GetReconciliationSnapshot(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of reconciliation snapshots.
    /// </summary>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="searchQuery">Search query for filtering results.</param>
    /// <param name="countryId">The country ID</param>
    /// <returns>Returns a paginated list of reconciliation snapshots.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<PayrollReconciliationSnapshotDto>>))]
    public async Task<IResult> GetReconciliationSnapshots([FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null, [FromQuery] Guid? countryId = null)
    {
        var result = await repository.GetReconciliationSnapshots(page, pageSize, searchQuery, countryId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

}