using APP.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.ProductionSchedules;

namespace API.Controllers;

[Route("api/v{version:apiVersion}/production-schedule")]
[ApiController]
public class ProductionScheduleController(IProductionScheduleRepository repository) : ControllerBase
{
    #region Master Production Schedule

    /// <summary>
    /// Creates a new Master Production Schedule.
    /// </summary>
    /// <param name="request">The CreateMasterProductionScheduleRequest object.</param>
    /// <returns>Returns the ID of the created Master Production Schedule.</returns>
    [HttpPost("master")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateMasterProductionSchedule([FromBody] CreateMasterProductionScheduleRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.CreateMasterProductionSchedule(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a specific Master Production Schedule by its ID.
    /// </summary>
    /// <param name="masterScheduleId">The ID of the Master Production Schedule.</param>
    /// <returns>Returns the Master Production Schedule.</returns>
    [HttpGet("master/{masterScheduleId}")]
    //[Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MasterProductionScheduleDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetMasterProductionSchedule(Guid masterScheduleId)
    {
        var result = await repository.GetMasterProductionSchedule(masterScheduleId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of Master Production Schedules.
    /// </summary>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="searchQuery">Search query for filtering results.</param>
    /// <returns>Returns a paginated list of Master Production Schedules.</returns>
    [HttpGet("masters")]
    //[Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<MasterProductionScheduleDto>>))]
    public async Task<IResult> GetMasterProductionSchedules([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchQuery = null)
    {
        var result = await repository.GetMasterProductionSchedules(page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates a specific Master Production Schedule.
    /// </summary>
    /// <param name="request">The UpdateMasterProductionScheduleRequest object containing updated data.</param>
    /// <param name="masterScheduleId">The ID of the Master Production Schedule to be updated.</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpPut("master/{masterScheduleId}")]
    //[Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateMasterProductionSchedule([FromBody] UpdateMasterProductionScheduleRequest request, Guid masterScheduleId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.UpdateMasterProductionSchedule(request, masterScheduleId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a specific Master Production Schedule.
    /// </summary>
    /// <param name="masterScheduleId">The ID of the Master Production Schedule to be deleted.</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpDelete("master/{masterScheduleId}")]
    //[Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteMasterProductionSchedule(Guid masterScheduleId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.DeleteMasterProductionSchedule(masterScheduleId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    #endregion

    #region Production Schedule

    /// <summary>
    /// Creates a new Production Schedule.
    /// </summary>
    /// <param name="request">The CreateProductionScheduleRequest object.</param>
    /// <returns>Returns the ID of the created Production Schedule.</returns>
    [HttpPost("schedule")]
    //[Authorize]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateProductionSchedule([FromBody] CreateProductionScheduleRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.CreateProductionSchedule(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a specific Production Schedule by its ID.
    /// </summary>
    /// <param name="scheduleId">The ID of the Production Schedule.</param>
    /// <returns>Returns the Production Schedule.</returns>
    [HttpGet("schedule/{scheduleId}")]
    //[Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProductionScheduleDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetProductionSchedule(Guid scheduleId)
    {
        var result = await repository.GetProductionSchedule(scheduleId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of Production Schedules.
    /// </summary>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="searchQuery">Search query for filtering results.</param>
    /// <returns>Returns a paginated list of Production Schedules.</returns>
    [HttpGet("schedules")]
    //[Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<ProductionScheduleDto>>))]
    public async Task<IResult> GetProductionSchedules([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchQuery = null)
    {
        var result = await repository.GetProductionSchedules(page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates a specific Production Schedule.
    /// </summary>
    /// <param name="request">The UpdateProductionScheduleRequest object containing updated data.</param>
    /// <param name="scheduleId">The ID of the Production Schedule to be updated.</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpPut("schedule/{scheduleId}")]
    //[Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateProductionSchedule([FromBody] UpdateProductionScheduleRequest request, Guid scheduleId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.UpdateProductionSchedule(request, scheduleId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a specific Production Schedule.
    /// </summary>
    /// <param name="scheduleId">The ID of the Production Schedule to be deleted.</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpDelete("schedule/{scheduleId}")]
    //[Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteProductionSchedule(Guid scheduleId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.DeleteProductionSchedule(scheduleId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    #endregion
}
