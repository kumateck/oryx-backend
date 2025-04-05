using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.ActivityLogs;
using DOMAIN.Entities.Procurement.Manufacturers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Route("api/v{version:apiVersion}/activity-log")]
[ApiController]
public class ActivityLogController(IActivityLogRepository repo) : ControllerBase
{
    /// <summary>
    /// Retrieves a paginated list of activity logs.
    /// </summary>
    /// <param name="filter">Filter for activity logs</param>
    /// <returns>Returns a paginated list of manufacturers.</returns>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<ActivityLogDto>>))]
    public async Task<IResult> GetManufacturers([FromQuery] ActivityLogFilter filter)
    {
        var result = await repo.GetActivityLogs(filter);
        return TypedResults.Ok(result);
    }
}