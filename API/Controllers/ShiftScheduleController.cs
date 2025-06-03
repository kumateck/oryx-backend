using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.ShiftAssignments;
using DOMAIN.Entities.ShiftSchedules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/shift-schedules")]
[Authorize]
public class ShiftScheduleController(IShiftScheduleRepository repository): ControllerBase
{
    /// <summary>
    /// Creates a new shift schedule.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateShiftSchedule([FromBody] CreateShiftScheduleRequest request)
    {
        var result = await repository.CreateShiftSchedule(request);
        return result.IsSuccess ? TypedResults.Ok(result.Value): result.ToProblemDetails();
    }

    /// <summary>
    /// Assigns employees a shift schedule
    /// </summary>
    [HttpPost("assign")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> AssignShiftToEmployee([FromBody] AssignShiftRequest request)
    {
        var result = await repository.AssignEmployeesToShift(request);
        return result.IsSuccess ? TypedResults.Ok() : result.ToProblemDetails();
    }

    /// <summary>
    /// Returns a paginated list of shift schedules based on a search criteria.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<ShiftScheduleDto>>))]
    public async Task<IResult> GetShiftSchedules([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchQuery = null)
    {
        var result = await repository.GetShiftSchedules(page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Returns a shift schedule by its ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ShiftScheduleDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetShiftSchedule([FromRoute] Guid id)
    {
        var result = await repository.GetShiftSchedule(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value): result.ToProblemDetails();
    }

    /// <summary>
    /// Returns the schedule for a specified date range
    /// </summary>
    [HttpGet("{scheduleId:guid}/view")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<ShiftScheduleDto>))]
    public async Task<IResult> GetShiftScheduleRangeView([FromRoute] Guid scheduleId,[FromQuery] DateTime startDate,
       [FromQuery] DateTime endDate)
    {
        var result = await repository.GetShiftScheduleRangeView(scheduleId, startDate, endDate);
        return result.IsSuccess ? TypedResults.Ok(result.Value): result.ToProblemDetails();
    }
    
    /// <summary>
    /// Updates the details of an existing shift schedule.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent, Type = typeof(ShiftScheduleDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateShiftSchedule([FromRoute] Guid id, [FromBody] CreateShiftScheduleRequest request)
    {
        var result = await repository.UpdateShiftSchedule(id, request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    [HttpPut("{id:guid}/update-schedule")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateAssignments(Guid id, UpdateShiftAssignment request)
    {
        var result = await repository.UpdateShiftAssignment(id, request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a specific shift schedule by its ID.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteShiftSchedule([FromRoute] Guid id)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.DeleteShiftSchedule(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}