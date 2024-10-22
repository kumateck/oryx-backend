using APP.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;

namespace API.Controllers;

[Route("api/v{version:apiVersion}/material")] 
[ApiController] 
public class MaterialController(IMaterialRepository repository) : ControllerBase 
{
    /// <summary>
    /// Creates a new material.
    /// </summary>
    /// <param name="request">The CreateMaterialRequest object.</param>
    /// <returns>Returns the ID of the created material.</returns>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateMaterial([FromBody] CreateMaterialRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.CreateMaterial(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a specific material by its ID.
    /// </summary>
    /// <param name="materialId">The ID of the material.</param>
    /// <returns>Returns the material details.</returns>
    [HttpGet("{materialId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MaterialDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetMaterial(Guid materialId)
    {
        var result = await repository.GetMaterial(materialId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of materials.
    /// </summary>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="searchQuery">Search query for filtering results.</param>
    /// <returns>Returns a paginated list of materials.</returns>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<MaterialDto>>))]
    public async Task<IResult> GetMaterials([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchQuery = null)
    {
        var result = await repository.GetMaterials(page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates a specific material by its ID.
    /// </summary>
    /// <param name="request">The UpdateMaterialRequest object containing updated material data.</param>
    /// <param name="materialId">The ID of the material to be updated.</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpPut("{materialId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateMaterial([FromBody] CreateMaterialRequest request, Guid materialId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.UpdateMaterial(request, materialId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a specific material by its ID.
    /// </summary>
    /// <param name="materialId">The ID of the material to be deleted.</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpDelete("{materialId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteMaterial(Guid materialId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.DeleteMaterial(materialId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    } 

    /// <summary>
    /// Checks the stock level of a specific material by its ID.
    /// </summary>
    /// <param name="materialId">The ID of the material.</param>
    /// <returns>Returns the current stock level of the material.</returns>
    [HttpGet("{materialId}/stock-level")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(int))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> CheckStockLevel(Guid materialId)
    {
        var result = await repository.CheckStockLevel(materialId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /*/// <summary>
    /// Checks if a requisition can be fulfilled for a specific material.
    /// </summary>
    /// <param name="materialId">The ID of the material.</param>
    /// <param name="requisitionId">The ID of the requisition.</param>
    /// <returns>Returns a boolean indicating if the requisition can be fulfilled.</returns>
    [HttpGet("{materialId}/can-fulfill-requisition/{requisitionId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(bool))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> CanFulfillRequisition(Guid materialId, Guid requisitionId)
    {
        var result = await repository.CanFulfillRequisition(materialId, requisitionId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }*/

    /// <summary>
    /// Creates a new material batch.
    /// </summary>
    /// <param name="request">The CreateMaterialBatchRequest object.</param>
    /// <returns>Returns the ID of the created material batch.</returns>
    [HttpPost("batch")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateMaterialBatch([FromBody] List<CreateMaterialBatchRequest> request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.CreateMaterialBatch(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a specific material batch by its ID.
    /// </summary>
    /// <param name="batchId">The ID of the material batch.</param>
    /// <returns>Returns the material batch details.</returns>
    [HttpGet("batch/{batchId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MaterialBatchDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetMaterialBatch(Guid batchId)
    {
        var result = await repository.GetMaterialBatch(batchId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of material batches.
    /// </summary>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="searchQuery">Search query for filtering results.</param>
    /// <returns>Returns a paginated list of material batches.</returns>
    [HttpGet("batches")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<MaterialBatchDto>>))]
    public async Task<IResult> GetMaterialBatches([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchQuery = null)
    {
        var result = await repository.GetMaterialBatches(page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}
