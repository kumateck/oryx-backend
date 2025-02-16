using APP.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Warehouses;

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
    /// <param name="kind">The kind of material being requested</param>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="searchQuery">Search query for filtering results.</param>
    /// <returns>Returns a paginated list of materials.</returns>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<MaterialDto>>))]
    public async Task<IResult> GetMaterials([FromQuery] MaterialKind kind, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchQuery = null)
    {
        var result = await repository.GetMaterials(page, pageSize, searchQuery, kind);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Retrieves a list of all material categories.
    /// </summary>
    /// <param name="materialKind">The kind of material being requested</param>
    /// <returns>Returns a paginated list of material categories.</returns>
    [HttpGet("category")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<MaterialCategoryDto>))]
    public async Task<IResult> GetMaterialCategories([FromQuery] MaterialKind? materialKind = null)
    {
        var result = await repository.GetMaterialCategories(materialKind);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Retrieves a list of all materials.
    /// </summary>
    /// <returns>Returns a paginated list of materials.</returns>
    [HttpGet("all")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<MaterialDto>))]
    public async Task<IResult> GetMaterials()
    {
        var result = await repository.GetMaterials();
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
    
    /// <summary>
    /// Retrieves a list of material batches by material ID.
    /// </summary>
    /// <param name="materialId">The ID of the material.</param>
    /// <returns>Returns a list of material batches.</returns>
    [HttpGet("{materialId}/batches")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<MaterialBatchDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetMaterialBatchesByMaterialId(Guid materialId)
    {
        var result = await repository.GetMaterialBatchesByMaterialId(materialId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Retrieves the stock of materials in transit.
    /// </summary>
    /// <param name="materialId">The ID of the material.</param>
    /// <returns>Returns the stock of material in transit.</returns>
    [HttpGet("{materialId}/in-transit")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(decimal))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetMaterialsInTransit(Guid materialId)
    {
        var result = await repository.GetMaterialsInTransit(materialId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

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
    [HttpGet("batch")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<MaterialBatchDto>>))]
    public async Task<IResult> GetMaterialBatches([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchQuery = null)
    {
        var result = await repository.GetMaterialBatches(page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Moves a material batch from one location to another.
    /// </summary>
    /// <param name="request">The move material to location request object</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpPost("batch/move")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> MoveMaterialBatch([FromBody] MoveMaterialBatchRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.MoveMaterialBatchByMaterial(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves the total stock of a material in a specific warehouse.
    /// </summary>
    /// <param name="materialId">The ID of the material.</param>
    /// <param name="warehouseId">The ID of the warehouse.</param>
    /// <returns>Returns the total stock quantity of the material in the specified warehouse.</returns>
    [HttpGet("{materialId}/stock/{warehouseId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(int))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetWarehouseStock(Guid materialId, Guid warehouseId)
    {
        var result = await repository.GetMaterialStockInWarehouse(materialId, warehouseId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Consumes a specified quantity of a material at a location.
    /// </summary>
    [HttpPost("batch/consume")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> ConsumeMaterialAtLocation(Guid batchId, Guid locationId, int quantity)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.ConsumeMaterialAtLocation(batchId, locationId, quantity, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves the stock levels across all warehouses for a specific material.
    /// </summary>
    /// <param name="materialId"> The id of the material</param>
    /// <returns></returns>
    [HttpGet("{materialId}/stock/across-warehouses")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<WarehouseStockDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetMaterialStockAcrossWarehouses(Guid materialId)
    {
        var result = await repository.GetMaterialStockAcrossWarehouses(materialId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Imports materials from an Excel file.
    /// </summary>
    /// <param name="file">The uploaded Excel file containing materials.</param>
    /// <param name="kind">The kind of materials being imported.</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpPost("upload")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> UploadMaterials(IFormFile file, [FromQuery] MaterialKind kind)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.ImportMaterialsFromExcel(file, kind);

        return result.IsSuccess
            ? TypedResults.NoContent() : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Updates the batch status of the specified material batches.
    /// </summary>
    /// <param name="request">The UpdateBatchStatusRequest object.</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpPut("batch/status")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateBatchStatus([FromBody] UpdateBatchStatusRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.UpdateBatchStatus(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}
