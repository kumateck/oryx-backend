using APP.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.ProductionSchedules.Packing;
using DOMAIN.Entities.ProductionSchedules.StockTransfers;
using DOMAIN.Entities.ProductionSchedules.StockTransfers.Request;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.Requisitions;
using SHARED.Requests;

namespace API.Controllers;

[Route("api/v{version:apiVersion}/production-schedule")]
[ApiController]
public class ProductionScheduleController(IProductionScheduleRepository repository) : ControllerBase
{
    #region Production Schedule

    /// <summary>
    /// Creates a new Production Schedule.
    /// </summary>
    /// <param name="request">The CreateProductionScheduleRequest object.</param>
    /// <returns>Returns the ID of the created Production Schedule.</returns>
    [HttpPost]
    [Authorize]
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
    [Authorize]
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
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<ProductionScheduleDto>>))]
    public async Task<IResult> GetProductionSchedules([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchQuery = null)
    {
        var result = await repository.GetProductionSchedules(page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Retrieves the details of a specific product in a Production Schedule.
    /// </summary>
    /// <param name="productionScheduleId">The ID of the Production Schedule.</param>
    /// <param name="productId">The ID of the Product.</param>
    /// <returns>Returns the details of the product in the Production Schedule.</returns>
    [HttpGet("{productionScheduleId}/product/{productId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProductionScheduleProductDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetProductDetailsInProductionSchedule(Guid productionScheduleId, Guid productId)
    {
        var result = await repository.GetProductDetailsInProductionSchedule(productionScheduleId, productId);
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
    
    [HttpGet("material-stock/{productionScheduleId}/{productId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<ProductionScheduleProcurementDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetRequiredMaterialStock(Guid productionScheduleId, Guid productId, [FromQuery] MaterialRequisitionStatus? status = null)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.CheckMaterialStockLevelsForProductionSchedule(productionScheduleId, productId, status);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    [HttpGet("package-material-stock/{productionScheduleId}/{productId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<ProductionScheduleProcurementPackageDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetRequiredPackageMaterialStock(Guid productionScheduleId, Guid productId, [FromQuery] MaterialRequisitionStatus? status = null)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.CheckPackageMaterialStockLevelsForProductionSchedule(productionScheduleId, productId, status);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
        /// <summary>
    /// Retrieves materials with insufficient stock for a given production schedule and product.
    /// </summary>
    /// <param name="productionScheduleId">The ID of the Production Schedule.</param>
    /// <param name="productId">The ID of the Product.</param>
    /// <returns>Returns a list of materials with insufficient stock.</returns>
    [HttpGet("{productionScheduleId}/materials-with-insufficient-stock/{productId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<ProductionScheduleProcurementDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetMaterialsWithInsufficientStock(Guid productionScheduleId, Guid productId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.GetMaterialsWithInsufficientStock(productionScheduleId, productId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves packaging materials with insufficient stock for a given production schedule and product.
    /// </summary>
    /// <param name="productionScheduleId">The ID of the Production Schedule.</param>
    /// <param name="productId">The ID of the Product.</param>
    /// <returns>Returns a list of packaging materials with insufficient stock.</returns>
    [HttpGet("{productionScheduleId}/package-materials-with-insufficient-stock/{productId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<ProductionScheduleProcurementPackageDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetPackageMaterialsWithInsufficientStock(Guid productionScheduleId, Guid productId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.GetPackageMaterialsWithInsufficientStock(productionScheduleId, productId, Guid.Parse(userId));
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
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.StartProductionActivity(productionScheduleId, productId, Guid.Parse(userId));
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
    [HttpGet("activity/status-grouped")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Dictionary<string, List<ProductionActivityDto>>))]
    public async Task<IResult> GetProductionActivityGroupedByStatus()
    {
        var result = await repository.GetProductionActivityGroupedByStatus();
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Groups Production Activities by their current step.
    /// </summary>
    /// <returns>Returns a dictionary grouping Production Activities by their status.</returns>
    [HttpGet("activity/operation-grouped")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<ProductionActivityGroupResultDto>))]
    public async Task<IResult> GetProductionActivityGroupedByOperation()
    {
        var result = await repository.GetProductionActivityGroupedByOperation();
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
    [HttpGet("activity-step/status-grouped")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Dictionary<string, List<ProductionActivityStepDto>>))]
    public async Task<IResult> GetProductionActivityStepsGroupedByStatus()
    {
        var result = await repository.GetProductionActivityStepsGroupedByStatus();
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Groups Production Activity Steps by their operation.
    /// </summary>
    /// <returns>Returns a dictionary grouping Production Activity Steps by their status.</returns>
    [HttpGet("activity-step/operation-grouped")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Dictionary<string, List<ProductionActivityStepDto>>))]
    public async Task<IResult> GetProductionActivityStepsGroupedByOperation()
    {
        var result = await repository.GetProductionActivityStepsGroupedByOperation();
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    #endregion

    #region Manufacturing Record

    /// <summary>
    /// Creates a new batch manufacturing record.
    /// </summary>
    [HttpPost("manufacturing")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateBatchManufacturingRecord([FromBody] CreateBatchManufacturingRecord request)
    {
        var result = await repository.CreateBatchManufacturingRecord(request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
       
    [HttpGet("manufacturing/{productionId}/{productionScheduleId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BatchManufacturingRecordDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetBatchManufacturingRecordByProductionAndScheduleId(Guid productionId, Guid productionScheduleId)
    {
        var result = await repository.GetBatchManufacturingRecordByProductionAndScheduleId(productionId, productionScheduleId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    [HttpPost("finished-goods-transfer-note")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateFinishedGoodsTransferNote([FromBody] CreateFinishedGoodsTransferNoteRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.CreateFinishedGoodsTransferNote(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Retrieves a paginated list of finished good transfer notes
    /// </summary>
    [HttpGet("finished-goods-transfer-note")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<FinishedGoodsTransferNoteDto>>))]
    public async Task<IResult> GetFinishedGoodsTransferNotes(
        bool? onlyApproved = null,
        int page = 1,
        int pageSize = 10,
        string searchQuery = null)
    {
        var result = await repository.GetFinishedGoodsTransferNote(onlyApproved, page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Retrieves the details of a finished good transfer note
    /// </summary>
    [HttpGet("finished-goods-transfer-note/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FinishedGoodsTransferNoteDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetFinishedGoodsTransferNote([FromRoute] Guid id)
    {
        var result = await repository.GetFinishedGoodsTransferNote(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Retrieves the details of a finished good transfer note by product Id
    /// </summary>
    [HttpGet("finished-goods-transfer-note/{productId:guid}/product")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<FinishedGoodsTransferNoteDto>>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetFinishedGoodsTransferNoteByProduct([FromRoute] Guid productId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null)
    {
        var result = await repository.GetFinishedGoodsTransferNoteByProduct(page, pageSize, searchQuery, productId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPut("finished-goods-transfer-note/{id:guid}/approve")]
    public async Task<IResult> ApproveTransferNote([FromRoute] Guid id, [FromBody] ApproveTransferNoteRequest quantityReceived)
    {
        var result = await repository.ApproveTransferNote(id, quantityReceived);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates the details of a transfer note.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateFinishedGoodsTransferNote([FromRoute] Guid id, [FromBody] CreateFinishedGoodsTransferNoteRequest request)
    {
        var result = await repository.UpdateTransferNote(id,request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of batch manufacturing records.
    /// </summary>
    [HttpGet("manufacturing")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<BatchManufacturingRecordDto>>))]
    public async Task<IResult> GetBatchManufacturingRecords([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchQuery = null)
    {
        var result = await repository.GetBatchManufacturingRecords(page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a specific batch manufacturing record by its ID.
    /// </summary>
    [HttpGet("manufacturing/{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BatchManufacturingRecordDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetBatchManufacturingRecord(Guid id)
    {
        var result = await repository.GetBatchManufacturingRecord(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates a specific batch manufacturing record by its ID.
    /// </summary>
    [HttpPut("manufacturing/{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateBatchManufacturingRecord([FromBody] UpdateBatchManufacturingRecord request, Guid id)
    {
        var result = await repository.UpdateBatchManufacturingRecord(request, id);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Issues a specific batch manufacturing record by its ID.
    /// </summary>
    [HttpPut("manufacturing/issue/{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> IssueBatchManufacturingRecord(Guid id)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.IssueBatchManufacturingRecord(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Creates a new batch packaging record.
    /// </summary>
    [HttpPost("packaging")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateBatchPackagingRecord([FromBody] CreateBatchPackagingRecord request)
    {
        var result = await repository.CreateBatchPackagingRecord(request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of batch packaging records.
    /// </summary>
    [HttpGet("packaging")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<BatchPackagingRecordDto>>))]
    public async Task<IResult> GetBatchPackagingRecords([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchQuery = null)
    {
        var result = await repository.GetBatchPackagingRecords(page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a specific batch packaging record by its ID.
    /// </summary>
    [HttpGet("packaging/{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BatchPackagingRecordDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetBatchPackagingRecord(Guid id)
    {
        var result = await repository.GetBatchPackagingRecord(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates a specific batch packaging record by its ID.
    /// </summary>
    [HttpPut("packaging/{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateBatchPackagingRecord([FromBody] UpdateBatchPackagingRecord request, Guid id)
    {
        var result = await repository.UpdateBatchPackagingRecord(request, id);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Issues a specific batch packing record by its ID.
    /// </summary>
    [HttpPut("packaging/issue/{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> IssueBatchPackagingRecord(Guid id)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.IssueBatchPackagingRecord(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    #endregion

    #region Stock Transfer

    /// <summary>
    /// Creates a new Stock Transfer.
    /// </summary>
    [HttpPost("stock-transfer")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateStockTransfer([FromBody] CreateStockTransferRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.CreateStockTransfer(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a list of Stock Transfers with optional filters.
    /// </summary>
    [HttpGet("stock-transfer")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<StockTransferDto>))]
    public async Task<IResult> GetStockTransfers([FromQuery] Guid? fromDepartmentId = null, [FromQuery] Guid? toDepartmentId = null, [FromQuery] Guid? materialId = null)
    {
        var result = await repository.GetStockTransfers(fromDepartmentId, toDepartmentId, materialId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Retrieves a list of Stock Transfers with optional filters.
    /// </summary>
    [HttpGet("stock-transfer/in-bound")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<DepartmentStockTransferDto>>))]
    public async Task<IResult> GetInBoundStockTransfers([FromQuery] int page = 1, [FromQuery] int pageSize = 10, 
        [FromQuery] string searchQuery = null, 
        [FromQuery] StockTransferStatus? status = null, 
        [FromQuery] Guid? toDepartmentId = null)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.GetIncomingStockTransferRequestForUserDepartment(Guid.Parse(userId), page, pageSize, searchQuery, status, toDepartmentId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Retrieves a list of Stock Transfers with optional filters.
    /// </summary>
    [HttpGet("stock-transfer/out-bound")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<DepartmentStockTransferDto>>))]
    public async Task<IResult> GetOutBoundStockTransfers([FromQuery] int page = 1, [FromQuery] int pageSize = 10, 
        [FromQuery] string searchQuery = null, 
        [FromQuery] StockTransferStatus? status = null, 
        [FromQuery] Guid? fromDepartmentId = null)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.GetOutgoingStockTransferRequestForUserDepartment(Guid.Parse(userId), page, pageSize, searchQuery, status, fromDepartmentId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Get a Stock Transfer by ID.
    /// </summary>
    [HttpGet("stock-transfer/{stockTransferId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DepartmentStockTransferDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> GetStockTransfer(Guid stockTransferId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.GetStockTransferSource(stockTransferId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Approves a Stock Transfer.
    /// </summary>
    [HttpPut("stock-transfer/approve/{stockTransferId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> ApproveStockTransfer(Guid stockTransferId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.ApproveStockTransfer(stockTransferId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
    
    /// <summary>
        /// Rejects a Stock Transfer.
    /// </summary>
    [HttpPut("stock-transfer/reject/{stockTransferId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> RejectStockTransfer(Guid stockTransferId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.RejectStockTransfer(stockTransferId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Gets a list of material batches to fulfill a Stock Transfer.
    /// </summary>
    [HttpGet("stock-transfer/batch/{stockTransferId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<BatchToSupply>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> BatchesForStockTransfer(Guid stockTransferId)
    {
        var result = await repository.BatchesToSupplyForStockTransfer(stockTransferId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Issues a Stock Transfer with batch selection.
    /// </summary>
    [HttpPut("stock-transfer/issue/{stockTransferId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> IssueStockTransfer(Guid stockTransferId, [FromBody] List<BatchTransferRequest> batches)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.IssueStockTransfer(stockTransferId, batches,Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    #endregion
    
     #region Final Packing

    /// <summary>
    /// Creates a new Final Packing entry.
    /// </summary>
    /// <param name="request">The CreateFinalPacking object.</param>
    /// <returns>Returns the ID of the created Final Packing entry.</returns>
    [HttpPost("final-packing")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateFinalPacking([FromBody] CreateFinalPacking request)
    {
        var result = await repository.CreateFinalPacking(request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a specific Final Packing by its ID.
    /// </summary>
    /// <param name="finalPackingId">The ID of the Final Packing.</param>
    /// <returns>Returns the Final Packing details.</returns>
    [HttpGet("final-packing/{finalPackingId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FinalPackingDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetFinalPacking(Guid finalPackingId)
    {
        var result = await repository.GetFinalPacking(finalPackingId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a Final Packing using Production Schedule ID and Product ID.
    /// </summary>
    /// <param name="productionScheduleId">The Production Schedule ID.</param>
    /// <param name="productId">The Product ID.</param>
    /// <returns>Returns the Final Packing details.</returns>
    [HttpGet("final-packing/{productionScheduleId}/{productId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FinalPackingDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetFinalPackingByScheduleAndProduct(Guid productionScheduleId, Guid productId)
    {
        var result = await repository.GetFinalPackingByScheduleAndProduct(productionScheduleId, productId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of Final Packing entries.
    /// </summary>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="searchQuery">Search query for filtering results.</param>
    /// <returns>Returns a paginated list of Final Packing entries.</returns>
    [HttpGet("final-packing")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<FinalPackingDto>>))]
    public async Task<IResult> GetFinalPackings([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchQuery = null)
    {
        var result = await repository.GetFinalPackings(page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates an existing Final Packing entry.
    /// </summary>
    /// <param name="request">The CreateFinalPacking object containing updated data.</param>
    /// <param name="finalPackingId">The ID of the Final Packing to be updated.</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpPut("final-packing/{finalPackingId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateFinalPacking([FromBody] CreateFinalPacking request, Guid finalPackingId)
    {
        var result = await repository.UpdateFinalPacking(request, finalPackingId);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a specific Final Packing entry.
    /// </summary>
    /// <param name="finalPackingId">The ID of the Final Packing to be deleted.</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpDelete("final-packing/{finalPackingId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteFinalPacking(Guid finalPackingId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.DeleteFinalPacking(finalPackingId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Retrieves a Stock Requisition for Packaging based on Production Schedule and Product ID.
    /// </summary>
    /// <param name="productionScheduleId">The Production Schedule ID.</param>
    /// <param name="productId">The Product ID.</param>
    /// <returns>Returns the Stock Requisition for Packaging.</returns>
    [HttpGet("stock-requisition/raw/{productionScheduleId}/{productId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(RequisitionDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetStockRequisitionForRaw(Guid productionScheduleId, Guid productId)
    {
        var result = await repository.GetStockRequisitionForRaw(productionScheduleId, productId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Retrieves a Stock Requisition for Packaging based on Production Schedule and Product ID.
    /// </summary>
    /// <param name="productionScheduleId">The Production Schedule ID.</param>
    /// <param name="productId">The Product ID.</param>
    /// <returns>Returns the Stock Requisition for Packaging.</returns>
    [HttpGet("stock-requisition/package/{productionScheduleId}/{productId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(RequisitionDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetStockRequisitionForPackaging(Guid productionScheduleId, Guid productId)
    {
        var result = await repository.GetStockRequisitionForPackaging(productionScheduleId, productId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    #endregion

    #region Material Return Note

    /// <summary>
    /// Returns unused materials before production begins.
    /// </summary>
    /// <param name="productionScheduleId">The ID of the Production Schedule.</param>
    /// <param name="productId">The ID of the Product.</param>
    /// <param name="reason">The reason for cancelling the production</param>
    /// <returns>Returns a success result if materials were returned successfully.</returns>
    [HttpPost("return-before-production")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> ReturnBeforeProduction([FromQuery] Guid productionScheduleId, [FromQuery] Guid productId, [FromQuery] string reason)
    {
        var result = await repository.ReturnStockBeforeProductionBegins(productionScheduleId, productId, reason);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Returns leftover materials after production ends.
    /// </summary>
    /// <param name="productionScheduleId">The ID of the Production Schedule.</param>
    /// <param name="productId">The ID of the Product.</param>
    /// <param name="returns">The list of partially used materials to return.</param>
    /// <returns>Returns a success result if leftovers were recorded successfully.</returns>
    [HttpPost("return-after-production")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> ReturnAfterProduction([FromQuery] Guid productionScheduleId, [FromQuery] Guid productId, [FromBody] List<PartialMaterialToReturn> returns)
    {
        var result = await repository.ReturnLeftOverStockAfterProductionEnds(productionScheduleId, productId, returns);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of Material Return Notes.
    /// </summary>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="searchQuery">Search query for filtering results.</param>
    /// <returns>Returns a paginated list of Material Return Notes.</returns>
    [HttpGet("material-return-note")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<MaterialReturnNoteDto>>))]
    public async Task<IResult> GetMaterialReturnNotes([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchQuery = "")
    {
        var result = await repository.GetMaterialReturnNotes(page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a specific Material Return Note by its ID.
    /// </summary>
    /// <param name="materialReturnNoteId">The ID of the Material Return Note.</param>
    /// <returns>Returns the Material Return Note.</returns>
    [HttpGet("material-return-note/{materialReturnNoteId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MaterialReturnNoteDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetMaterialReturnNoteById(Guid materialReturnNoteId)
    {
        var result = await repository.GetMaterialReturnNoteById(materialReturnNoteId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Retrieves a specific Material Return Note by its ID.
    /// </summary>
    /// <param name="materialReturnNoteId">The ID of the Material Return Note.</param>
    /// <returns>Returns the Material Return Note.</returns>
    [HttpPut("material-return-note/complete/{materialReturnNoteId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> CompleteMaterialReturnNoteById(Guid materialReturnNoteId)
    {
        var result = await repository.CompleteMaterialReturn(materialReturnNoteId);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    #endregion
    
    #region Production Extra Packing

    /// <summary>
    /// Creates new Extra Packing entries for a given Production Schedule and Product.
    /// </summary>
    /// <param name="productionScheduleId">The ID of the Production Schedule.</param>
    /// <param name="productId">The ID of the Product.</param>
    /// <param name="extraPackings">List of Extra Packing details to create.</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpPost("extra-packing/{productionScheduleId}/{productId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateExtraPacking(Guid productionScheduleId, Guid productId, [FromBody] List<CreateProductionExtraPacking> extraPackings)
    {
        var result = await repository.CreateExtraPacking(productionScheduleId, productId, extraPackings);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of Extra Packing entries.
    /// </summary>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="searchQuery">Search query for filtering results.</param>
    /// <returns>Returns a paginated list of Extra Packing entries.</returns>
    [HttpGet("extra-packing")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<ProductionExtraPackingWithBatchesDto>>))]
    public async Task<IResult> GetProductionExtraPackings([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchQuery = "")
    {
        var result = await repository.GetProductionExtraPackings(page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a specific Extra Packing entry by ID, including associated batches.
    /// </summary>
    /// <param name="productionExtraPackingId">The ID of the Extra Packing.</param>
    /// <returns>Returns the Extra Packing with batches.</returns>
    [HttpGet("extra-packing/{productionExtraPackingId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProductionExtraPackingWithBatchesDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetProductionExtraPackingById(Guid productionExtraPackingId)
    {
        var result = await repository.GetProductionExtraPackingById(productionExtraPackingId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a specific Extra Packing entry by ID, including associated batches.
    /// </summary>
    /// <param name="productionScheduleId">The production schedule Id linked to the extra packing></param>
    /// <param name="productId">The product Id linked to the extra paccking</param>
    /// <returns>Returns the Extra Packing with batches.</returns>
    [HttpGet("extra-packing/by-product/{productionScheduleId}/{productId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<ProductionExtraPackingWithBatchesDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetProductionExtraPackingByProduct(Guid productionScheduleId, Guid productId)
    {
        var result = await repository.GetProductionExtraPackingByProduct(productionScheduleId, productId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves batches that can be supplied for a specific Extra Packing Material.
    /// </summary>
    /// <param name="extraPackingMaterialId">The ID of the Extra Packing Material.</param>
    /// <returns>Returns a list of batches to supply.</returns>
    [HttpGet("extra-packing/batches-to-supply/{extraPackingMaterialId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<BatchToSupply>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> BatchesToSupplyForExtraPackingMaterial(Guid extraPackingMaterialId)
    {
        var result = await repository.BatchesToSupplyForExtraPackingMaterial(extraPackingMaterialId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Approves an Extra Packing entry with the specified batch transfers.
    /// </summary>
    /// <param name="productionExtraPackingId">The ID of the Extra Packing.</param>
    /// <param name="batches">The list of batches for approval.</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpPost("extra-packing/approve/{productionExtraPackingId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> ApproveProductionExtraPacking(Guid productionExtraPackingId, [FromBody] List<BatchTransferRequest> batches)
    {
        var userIdStr = HttpContext.Items["Sub"] as string;
        if (string.IsNullOrEmpty(userIdStr)) return TypedResults.Unauthorized();

        var result = await repository.ApproveProductionExtraPacking(productionExtraPackingId, batches, Guid.Parse(userIdStr));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Retrieves a list of approved products
    /// </summary>
    [HttpGet("approved-products")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<ApprovedProductDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> GetApprovedProducts()
    {
        var result = await repository.GetApprovedProducts();
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    
    /// <summary>
    /// Retrieves details of an approved product
    /// </summary>
    [HttpGet("approved-products/{productId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<FinishedGoodsTransferNoteDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> GetApprovedProductDetails([FromRoute] Guid productId)
    {
        var result = await repository.GetApprovedProductDetails(productId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    #endregion

    #region Report

    /// <summary>
    /// Retrieves a summary report of Production Schedules.
    /// </summary>
    /// <param name="filter">The filter criteria for the report.</param>
    /// <returns>Returns a list of Production Schedule summary report DTOs.</returns>
    [HttpGet("summary-report")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<ProductionScheduleReportDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> GetProductionScheduleSummaryReport([FromQuery] ProductionScheduleReportFilter filter)
    {
        var result = await repository.GetProductionScheduleSummaryReport(filter);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Retrieves a detailed report of Production Schedules.
    /// </summary>
    /// <param name="filter">The filter criteria for the report.</param>
    /// <returns>Returns a list of Production Schedule summary report DTOs.</returns>
    [HttpGet("detailed-report")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<ProductionScheduleDetailedReportDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> GetProductionScheduleDetailedReport([FromQuery] ProductionScheduleReportFilter filter)
    {
        var result = await repository.GetProductionScheduleDetailedReport(filter);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    #endregion
    
    
}
