using APP.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Products.Production;
using SHARED.Requests;

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
    [HttpPost]
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
    [HttpGet("{scheduleId}")]
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
    [HttpGet]
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
    [HttpPut("{scheduleId}")]
    [Authorize]
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
    [HttpDelete("{scheduleId}")]
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

    /// <summary>
    /// Gets a list of all Production Status.
    /// </summary>
    /// <remarks>
    /// This endpoint returns a list of all available production status along with their integer values.
    /// </remarks>
    /// <returns>A list of Naming Types with their corresponding value and name.</returns>
    /// <response code="200">Returns the list of production status</response>
    [HttpGet("production-status")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<TypeResponse>))]
    public IResult GetQuestionValidationTypes()
    {
        var types = Enum.GetValues(typeof(ProductionStatus))
            .Cast<ProductionStatus>()
            .Select(qt => new TypeResponse
            {
                Value = (int)qt,
                Name = qt.ToString()
            })
            .ToList();

        return TypedResults.Ok(types);
    }
    
    /// <summary>
    /// Retrieves the details of a specific Production Schedule, including procurement information.
    /// </summary>
    /// <param name="scheduleId">The ID of the Production Schedule.</param>
    /// <returns>Returns the details of the Production Schedule, including procurement information.</returns>
    [HttpGet("{scheduleId}/details")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<ProductionScheduleProcurementDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetProductionScheduleDetails(Guid scheduleId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.GetProductionScheduleDetail(scheduleId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    #endregion
    
     #region Production Activities

    /// <summary>
    /// Starts a Production Activity for a given Production Schedule and Product.
    /// </summary>
    /// <param name="productionScheduleId">The Production Schedule ID.</param>
    /// <param name="productId">The Product ID.</param>
    /// <returns>Returns the ID of the started Production Activity.</returns>
    [HttpPost("activity/start/{productionScheduleId}/{productId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> StartProductionActivity(Guid productionScheduleId, Guid productId)
    {
        var result = await repository.StartProductionActivity(productionScheduleId, productId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of Production Activities with optional filters.
    /// </summary>
    /// <param name="filter">Filter parameters for retrieving production activities.</param>
    /// <returns>Returns a paginated list of Production Activities.</returns>
    [HttpGet("activity")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<ProductionActivityDto>>))]
    public async Task<IResult> GetProductionActivities([FromQuery] ProductionFilter filter)
    {
        var result = await repository.GetProductionActivities(filter);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a specific Production Activity by its ID.
    /// </summary>
    /// <param name="productionActivityId">The ID of the Production Activity.</param>
    /// <returns>Returns the details of the Production Activity.</returns>
    [HttpGet("activity/{productionActivityId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProductionActivityDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetProductionActivityById(Guid productionActivityId)
    {
        var result = await repository.GetProductionActivityById(productionActivityId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a specific Production Activity by production schedule id and product id.
    /// </summary>
    /// <param name="productionScheduleId">The Production Schedule ID.</param>
    /// <param name="productId">The Product ID.</param>
    /// <returns>Returns the details of the Production Activity.</returns>
    [HttpGet("activity/{productionScheduleId}/{productId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProductionActivityDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetProductionActivityByProductionScheduleAndProductId(Guid productionScheduleId, Guid productId)
    {
        var result = await repository.GetProductionActivityByProductionScheduleIdAndProductId(productionScheduleId, productId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Groups Production Activities by their status.
    /// </summary>
    /// <returns>Returns a dictionary grouping Production Activities by their status.</returns>
    [HttpGet("activiy/grouped")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Dictionary<string, List<ProductionActivityDto>>))]
    public async Task<IResult> GetProductionActivityGroupedByStatus()
    {
        var result = await repository.GetProductionActivityGroupedByStatus();
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    #endregion

    #region Production Activity Steps

    /// <summary>
    /// Updates the status of a specific Production Activity Step.
    /// </summary>
    /// <param name="productionStepId">The ID of the Production Step.</param>
    /// <param name="status">The new status to set.</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpPut("activity-step/{productionStepId}/status")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateStatusOfProductionActivityStep(Guid productionStepId,[FromQuery]ProductionStatus status)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.UpdateStatusOfProductionActivityStep(productionStepId, status, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of Production Activity Steps with optional filters.
    /// </summary>
    /// <param name="filter">Filter parameters for retrieving production activity steps.</param>
    /// <returns>Returns a paginated list of Production Activity Steps.</returns>
    [HttpGet("activity-step")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<ProductionActivityStepDto>>))]
    public async Task<IResult> GetProductionActivitySteps([FromQuery] ProductionFilter filter)
    {
        var result = await repository.GetProductionActivitySteps(filter);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a specific Production Activity Step by its ID.
    /// </summary>
    /// <param name="productionActivityStepId">The ID of the Production Activity Step.</param>
    /// <returns>Returns the details of the Production Activity Step.</returns>
    [HttpGet("activity-step/{productionActivityStepId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProductionActivityStepDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetProductionActivityStepById(Guid productionActivityStepId)
    {
        var result = await repository.GetProductionActivityStepById(productionActivityStepId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Groups Production Activity Steps by their status.
    /// </summary>
    /// <returns>Returns a dictionary grouping Production Activity Steps by their status.</returns>
    [HttpGet("activity-step/grouped")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Dictionary<string, List<ProductionActivityStepDto>>))]
    public async Task<IResult> GetProductionActivityStepsGroupedByStatus()
    {
        var result = await repository.GetProductionActivityStepsGroupedByStatus();
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    #endregion
}
