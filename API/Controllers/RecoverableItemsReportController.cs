using APP.Extensions;
using APP.IRepository;
using DOMAIN.Entities.RecoverableItemsReports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("v{version:apiVersion}/recoverable-items-reports")]
[Authorize]
public class RecoverableItemsReportController(IRecoverableItemReportRepository repository) : ControllerBase
{
    /// <summary>
    /// Creates a new recoverable item report.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateRecoverableItemReport([FromBody] CreateRecoverableItemReportRequest request)
    {
        var result = await repository.CreateRecoverableItemReport(request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a list of recoverable item reports.
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<RecoverableItemReportDto>))]
    public async Task<IResult> GetRecoverableItemReports()
    {
        var result = await repository.GetRecoverableItemReport();
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}