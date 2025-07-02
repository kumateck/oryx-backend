using APP.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.Alerts;

namespace API.Controllers;

[Route("api/v{version:apiVersion}/alert")]
[ApiController]
public class AlertController(IAlertRepository repo) : ControllerBase
{
    /// <summary>
    /// Creates a new alert.
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateAlert([FromBody] CreateAlertRequest request)
    {
        var result = await repo.CreateAlert(request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a specific alert by ID.
    /// </summary>
    [HttpGet("{alertId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AlertDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetAlert(Guid alertId)
    {
        var result = await repo.GetAlert(alertId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of alerts.
    /// </summary>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<AlertDto>>))]
    public async Task<IResult> GetAlerts([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchQuery = null, [FromQuery] bool withDisabled = false)
    {
        var result = await repo.GetAlerts(page, pageSize, searchQuery, withDisabled);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Updates an existing alert.
    /// </summary>
    [HttpPut("{alertId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateAlert([FromBody] CreateAlertRequest request, Guid alertId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repo.UpdateAlert(request, Guid.Parse(userId), alertId);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Toggles the disabled status of an alert.
    /// </summary>
    [HttpPut("{id}/toggle-disable")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> ToggleDisable(Guid id)
    {
        var result = await repo.ToggleDisable(id);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}
