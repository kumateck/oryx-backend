using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Reports.Warehouse;
using DOMAIN.Entities.Warehouses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SHARED;

namespace API.Controllers;

[Route("api/v{version:apiVersion}/material")]
[ApiController]
[Authorize]
public class MaterialController(IMaterialRepository repository) : ControllerBase
{
    /// <summary>
    /// Creates a new material.
    /// </summary>
    /// <param name="request">The CreateMaterialRequest object.</param>
    /// <returns>Returns the ID of the created material.</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateMaterial([FromBody] CreateMaterialRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.CreateMaterial(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a specific material by its ID.
    /// </summary>
    /// <param name="materialId">The ID of the material.</param>
    /// <returns>Returns the material details.</returns>
    [HttpGet("{materialId}")]
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
    [ProducesResponseType(
        StatusCodes.Status200OK,
        Type = typeof(Paginateable<IEnumerable<MaterialDto>>)
    )]
    public async Task<IResult> GetMaterials(
        [FromQuery] MaterialKind kind,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null
    )
    {
        var result = await repository.GetMaterials(page, pageSize, searchQuery, kind);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of materials not linked to ARDs.
    /// </summary>
    /// <param name="kind">The kind of material being requested</param>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="searchQuery">Search query for filtering results.</param>
    /// <returns>Returns a paginated list of materials.</returns>
    [HttpGet("not-linked-to-ards")]
    [ProducesResponseType(
        StatusCodes.Status200OK,
        Type = typeof(Paginateable<IEnumerable<MaterialDto>>)
    )]
    public async Task<IResult> GetMaterialsNotLinkedToArd(
        [FromQuery] MaterialKind kind,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null
    )
    {
        var result = await repository.GetMaterialsNotLinkedToArd(page, pageSize, searchQuery, kind);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a list of all material categories.
    /// </summary>
    /// <param name="materialKind">The kind of material being requested</param>
    /// <returns>Returns a paginated list of material categories.</returns>
    [HttpGet("category")]
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
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateMaterial(
        [FromBody] CreateMaterialRequest request,
        Guid materialId
    )
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.UpdateMaterial(request, materialId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates the ReOrderLevel of a specific material by its ID.
    /// </summary>
    /// <param name="materialId">The ID of the material to be updated.</param>
    /// <param name="reOrderLevel">The new ReOrderLevel value.</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpPut("{materialId}/reorder-level")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateReOrderLevel(
        [FromRoute] Guid materialId,
        [FromBody] UpdateReOrderLevelRequest reOrderLevel
    )
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.UpdateReOrderLevel(
            materialId,
            reOrderLevel.ReOrderLevel,
            Guid.Parse(userId)
        );
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a specific material by its ID.
    /// </summary>
    /// <param name="materialId">The ID of the material to be deleted.</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpDelete("{materialId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteMaterial([FromRoute] Guid materialId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.DeleteMaterial(materialId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Checks the stock level of a specific material by its ID.
    /// </summary>
    /// <param name="materialId">The ID of the material.</param>
    /// <returns>Returns the current stock level of the material.</returns>
    [HttpGet("{materialId}/stock-level")]
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
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateMaterialBatch(
        [FromBody] List<CreateMaterialBatchRequest> request
    )
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.CreateMaterialBatch(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a specific material batch by its ID.
    /// </summary>
    /// <param name="batchId">The ID of the material batch.</param>
    /// <returns>Returns the material batch details.</returns>
    [HttpGet("batch/{batchId}")]
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
    [ProducesResponseType(
        StatusCodes.Status200OK,
        Type = typeof(Paginateable<IEnumerable<MaterialBatchDto>>)
    )]
    public async Task<IResult> GetMaterialBatches(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null
    )
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
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> MoveMaterialBatch([FromBody] MoveMaterialBatchRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.MoveMaterialBatchByMaterial(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    [HttpPut("batch/{batchId}/approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> ApproveMaterialBatch(Guid batchId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.ApproveMaterialBatch(batchId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves the total stock of a material in a specific warehouse.
    /// </summary>
    /// <param name="materialId">The ID of the material.</param>
    /// <param name="warehouseId">The ID of the warehouse.</param>
    /// <returns>Returns the total stock quantity of the material in the specified warehouse.</returns>
    [HttpGet("{materialId}/stock/{warehouseId}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(int))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetWarehouseStock(Guid materialId, Guid warehouseId)
    {
        var result = await repository.GetMassMaterialStockInWarehouse(materialId, warehouseId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Consumes a specified quantity of a material at a location.
    /// </summary>
    [HttpPost("batch/consume")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> ConsumeMaterialAtLocation(
        Guid batchId,
        Guid locationId,
        int quantity
    )
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.ConsumeMaterialAtLocation(
            batchId,
            locationId,
            quantity,
            Guid.Parse(userId)
        );
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves the stock levels across all warehouses for a specific material.
    /// </summary>
    /// <param name="materialId"> The id of the material</param>
    /// <returns></returns>
    [HttpGet("{materialId}/stock/across-warehouses")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<WarehouseStockDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetMaterialStockAcrossWarehouses(Guid materialId)
    {
        var result = await repository.GetMaterialStockAcrossWarehouses(materialId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves the stock levels across all warehouses for a specific material.
    /// </summary>
    /// <param name="materialId"> The id of the material</param>
    /// <param name="quantity">The minimum quantity of the stock the department should have.</param>
    /// <returns></returns>
    [HttpGet("{materialId}/department-stock/{quantity}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<DepartmentDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetDepartmentsWithEnoughStock(Guid materialId, decimal quantity)
    {
        var result = await repository.GetDepartmentsWithEnoughStock(materialId, quantity);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Imports materials from an Excel file.
    /// </summary>
    /// <param name="file">The uploaded Excel file containing materials.</param>
    /// <param name="kind">The kind of materials being imported.</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> UploadMaterials(IFormFile file, [FromQuery] MaterialKind kind)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.ImportMaterialsFromExcel(file, kind);

        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates the batch status of the specified material batches.
    /// </summary>
    /// <param name="request">The UpdateBatchStatusRequest object.</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpPut("batch/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateBatchStatus([FromBody] UpdateBatchStatusRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.UpdateBatchStatus(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Supplies a material batch to warehouse shelves.
    /// </summary>
    /// <param name="request">The SupplyMaterialBatchRequest object.</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpPost("batch/supply")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> SupplyMaterialBatchToWarehouse(
        [FromBody] SupplyMaterialBatchRequest request
    )
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.SupplyMaterialBatchToWarehouse(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok() : result.ToProblemDetails();
    }

    /// <summary>
    /// Moves a shelf material batch from one shelf to another.
    /// </summary>
    /// <param name="request">The MoveShelfMaterialBatchRequest object.</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpPost("batch/move/v2")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> MoveMaterialBatchV2([FromBody] MoveShelfMaterialBatchRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.MoveMaterialBatchV2(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok() : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of approved materials for a specific warehouse.
    /// </summary>
    /// <param name="kind">The kind of material needed.</param>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="searchQuery">Search query for filtering results.</param>
    /// <returns>Returns a paginated list of approved raw materials.</returns>
    [HttpGet("approved-materials")]
    [ProducesResponseType(
        StatusCodes.Status200OK,
        Type = typeof(Paginateable<IEnumerable<MaterialDetailsDto>>)
    )]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetApprovedRawMaterials(
        [FromQuery] MaterialKind kind,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null
    )
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.GetApprovedMaterials(
            page,
            pageSize,
            searchQuery,
            kind,
            Guid.Parse(userId)
        );
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of approved materials for a specific warehouse.
    /// </summary>
    /// <param name="kind">The kind of material needed.</param>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="searchQuery">Search query for filtering results.</param>
    /// <param name="warehouseId">The warehouse id for filtering results.</param>
    /// <param name="departmentId">The department id for filtering results.</param>
    /// <returns>Returns a paginated list of approved raw materials.</returns>
    [HttpGet("department/approved-materials")]
    [ProducesResponseType(
        StatusCodes.Status200OK,
        Type = typeof(Paginateable<IEnumerable<MaterialDetailsDto>>)
    )]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetApprovedRawMaterialsByDepartment(
        [FromQuery] Guid warehouseId,
        [FromQuery] Guid departmentId,
        [FromQuery] MaterialKind kind,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null
    )
    {
        var result = await repository.GetApprovedMaterialsByDepartment(
            page,
            pageSize,
            searchQuery,
            kind,
            warehouseId,
            departmentId
        );
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of approved materials for a specific warehouse.
    /// </summary>
    /// <param name="kind">The kind of material needed.</param>
    /// <param name="materialCategoryId">The material category id.</param>
    /// <param name="departmentId">The department id for filtering results.</param>
    /// <returns>Returns a paginated list of approved raw materials.</returns>
    [HttpGet("department/approved-materials/v2")]
    [ProducesResponseType(
        StatusCodes.Status200OK,
        Type = typeof(Paginateable<IEnumerable<MaterialDetailsDto>>)
    )]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetApprovedRawMaterialsByDepartment(
        [FromQuery] Guid? departmentId,
        [FromQuery] MaterialKind? kind,
        [FromQuery] Guid? materialCategoryId
    )
    {
        var result = await repository.GetApprovedMaterialsByDepartmentV2(
            kind,
            departmentId,
            materialCategoryId
        );
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of material batches by material ID for a specific warehouse.
    /// </summary>
    /// <param name="materialId">The ID of the material.</param>
    /// <param name="departmentId"></param>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="searchQuery">Search material</param>
    /// <returns>Returns a paginated list of material batches.</returns>
    [HttpGet("{materialId:guid}/batches/v2/{departmentId:guid}")]
    [ProducesResponseType(
        StatusCodes.Status200OK,
        Type = typeof(Paginateable<List<ShelfMaterialBatchDto>>)
    )]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetMaterialBatchesByMaterialIdV2(
        [FromRoute] Guid materialId,
        [FromRoute] Guid departmentId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null
    )
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.GetMaterialBatchesByMaterialIdV2(
            page,
            pageSize,
            materialId,
            departmentId,
            Guid.Parse(userId)
        );
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves the stock of a material in different warehouses.
    /// </summary>
    /// <param name="materialId">The ID of the material.</param>
    /// <returns>Returns the stock of the material in all warehouses.</returns>
    [HttpGet("{materialId}/stock/warehouses")]
    [ProducesResponseType(
        StatusCodes.Status200OK,
        Type = typeof(List<MaterialStockByWarehouseDto>)
    )]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetStockByWarehouse(Guid materialId)
    {
        var result = await repository.GetStockByWarehouse(materialId);
        return result is not null ? TypedResults.Ok(result) : TypedResults.NotFound();
    }

    /// <summary>
    /// Retrieves the stock of a material in different departments.
    /// </summary>
    /// <param name="materialId">The ID of the material.</param>
    /// <returns>Returns the stock of the material in all departments.</returns>
    [HttpGet("{materialId}/stock/departments")]
    [ProducesResponseType(
        StatusCodes.Status200OK,
        Type = typeof(List<MaterialStockByDepartmentDto>)
    )]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetStockByDepartment(Guid materialId)
    {
        var result = await repository.GetStockByDepartment(materialId);
        return result is not null ? TypedResults.Ok(result) : TypedResults.NotFound();
    }

    /// <summary>
    /// Creates a new material department.
    /// </summary>
    /// <param name="materialDepartments">The list of material departments to create.</param>
    /// <returns>Returns the result of the creation process.</returns>
    [HttpPost("department")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateMaterialDepartment(
        [FromBody] List<CreateMaterialDepartment> materialDepartments
    )
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.CreateMaterialDepartment(
            materialDepartments,
            Guid.Parse(userId)
        );
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a new material department.
    /// </summary>
    /// <param name="materialId">The material you want to unlink from your department</param>
    /// <returns>Returns the result of the creation process.</returns>
    [HttpDelete("department")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> RemoveMaterialDepartment([FromQuery] Guid materialId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.RemoveMaterialDepartment(Guid.Parse(userId), materialId);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Returns a list of materials that have not been linked.
    /// </summary>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="searchQuery">Search material</param>
    /// <param name="kind">The material kind to filter</param>
    /// <returns>Returns the materials that have not been linked.</returns>
    [HttpGet("department/not-linked")]
    [ProducesResponseType(
        StatusCodes.Status200OK,
        Type = typeof(Paginateable<IEnumerable<MaterialWithWarehouseStockDto>>)
    )]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> GetNotLinkedMaterials(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 5,
        [FromQuery] string searchQuery = null,
        [FromQuery] MaterialKind? kind = null
    )
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.GetMaterialsThatHaveNotBeenLinked(
            page,
            pageSize,
            searchQuery,
            kind,
            Guid.Parse(userId)
        );
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of material departments.
    /// </summary>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="searchQuery">Search query for filtering results.</param>
    /// <param name="kind">The material kind to filter</param>
    /// <param name="materialCategoryId">The material category</param>
    /// <param name="sortLabel">The label to which to sort by. Options are 'materialcode' 'categoryname'
    /// and 'materialname' "</param>
    /// <param name="sortDirection">The direction to sort the results. 1 for Ascending, 2 for Descending</param>
    /// <returns>Returns a paginated list of material departments.</returns>
    [HttpGet("department")]
    [ProducesResponseType(
        StatusCodes.Status200OK,
        Type = typeof(Paginateable<IEnumerable<MaterialDepartmentWithWarehouseStockDto>>)
    )]
    public async Task<IResult> GetMaterialDepartments(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null,
        [FromQuery] MaterialKind? kind = null,
        [FromQuery] Guid? materialCategoryId = null,
        [FromQuery] string sortLabel = "",
        [FromQuery] SortDirection? sortDirection = null
    )
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.GetMaterialDepartments(
            page,
            pageSize,
            searchQuery,
            kind,
            materialCategoryId,
            sortLabel,
            sortDirection,
            Guid.Parse(userId)
        );
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Return the uom associated with the material
    /// </summary>
    /// <param name="materialId">The material Id for which you need the uom</param>
    /// <returns>Returns the materials that have not been linked.</returns>
    [HttpGet("{materialId}/uom")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<UnitOfMeasureDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> GetUoMForMaterial(Guid materialId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.GetUnitOfMeasureForMaterialDepartment(
            materialId,
            Guid.Parse(userId)
        );
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of holding materials
    /// </summary>
    /// <param name="withProcessed">Filter to include transferred holding materials</param>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="searchQuery">Search query for filtering results.</param>
    /// <param name="kind">The material kind you want</param>
    /// <returns>Returns a paginated list of material departments.</returns>
    [HttpGet("holding")]
    [ProducesResponseType(
        StatusCodes.Status200OK,
        Type = typeof(Paginateable<IEnumerable<HoldingMaterialTransferDto>>)
    )]
    public async Task<IResult> GetMaterialDepartments(
        [FromQuery] bool withProcessed = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null,
        [FromQuery] MaterialKind? kind = null
    )
    {
        var departmentId = (string)HttpContext.Items["Department"];
        if (string.IsNullOrEmpty(departmentId))
            return TypedResults.Unauthorized();

        var result = await repository.GetHoldingMaterialTransfers(
            page,
            pageSize,
            searchQuery,
            withProcessed,
            Guid.Parse(departmentId),
            kind
        );
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Moves a shelf material batch from one shelf to another when being held.
    /// </summary>
    /// <param name="request">The MoveShelfMaterialBatchRequest object.</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpPost("holding/move")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> MoveMaterialBatchToWarehouseFromHolding(
        [FromBody] SupplyMaterialBatchFromHoldingRequest request
    )
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.MoveMaterialBatchToWarehouseFromHolding(
            request,
            Guid.Parse(userId)
        );
        return result.IsSuccess ? TypedResults.Ok() : result.ToProblemDetails();
    }

    [HttpPost("batches/import")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> ImportMaterialBatchesFromCsv(IFormFile file)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.ImportMaterialBatchesFromExcel(file, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a  list of material batches that have expired
    /// </summary>
    /// <returns>Returns a paginated list of material departments.</returns>
    [HttpGet("batches/expired")]
    [ProducesResponseType(
        StatusCodes.Status200OK,
        Type = typeof(Paginateable<IEnumerable<MaterialBatchDto>>)
    )]
    public async Task<IResult> GetExpiredMaterialBatches([FromQuery] MaterialFilter filter)
    {
        var result = await repository.GetExpiredMaterialBatches(filter);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Retrieves a  list of material batches that are about to expire
    /// </summary>
    /// <returns>Returns a paginated list of material departments.</returns>
    [HttpGet("batches/about-to-expire")]
    [ProducesResponseType(
        StatusCodes.Status200OK,
        Type = typeof(Paginateable<IEnumerable<MaterialBatchDto>>)
    )]
    public async Task<IResult> GetAboutToExpireMaterialBatches([FromQuery] MaterialFilter filter)
    {
        var result = await repository.GetAboutToExpireMaterialBatches(filter);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("material-specs/not-linked")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<MaterialDto>))]
    public async Task<IResult> GetMaterialsNotLinkedToSpec(
        [FromQuery] MaterialKind materialKind = 0
    )
    {
        var result = await repository.GetMaterialsNotLinkedToSpec(materialKind);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("rejects")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<MaterialDto>))]
    public async Task<IResult> GetMaterialRejects(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null,
        [FromQuery] MaterialKind? materialKind = null
    )
    {
        var result = await repository.GetMaterialRejected(
            page,
            pageSize,
            searchQuery,
            materialKind
        );
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves the total stock of a material in a specific warehouse.
    /// </summary>
    /// <param name="materialId">The ID of the material.</param>
    /// <param name="departmentId">Optional department Id</param>
    /// <param name="onlyAboutToExpire">Flag to include if only about to expire </param>
    /// <returns>Returns the total stock quantity of the material in the specified warehouse.</returns>
    [HttpGet("{materialId}/shelf")]
    [ProducesResponseType(
        StatusCodes.Status200OK,
        Type = typeof(IEnumerable<ShelfMaterialBatchDto>)
    )]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetShelfMaterialsAcrossWarehouses(
        [FromRoute] Guid materialId,
        [FromQuery] Guid? departmentId = null,
        [FromQuery] bool? onlyAboutToExpire = null
    )
    {
        var result = await repository.GetShelfMaterialsAcrossWarehouses(
            materialId,
            departmentId,
            onlyAboutToExpire
        );
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves the shelf material in stock
    /// </summary>
    /// <returns>Returns the total stock quantity of the material in the specified warehouse.</returns>
    [HttpGet("shelf")]
    [ProducesResponseType(
        StatusCodes.Status200OK,
        Type = typeof(IEnumerable<ShelfMaterialBatchDto>)
    )]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetShelfMaterialsAcrossWarehouses([FromQuery] string searchQuery)
    {
        var departmentId = (string)HttpContext.Items["Department"];
        if (string.IsNullOrEmpty(departmentId))
            return TypedResults.Unauthorized();

        var result = await repository.GetShelfMaterialsAcrossWarehouses(
            searchQuery,
            Guid.Parse(departmentId)
        );
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Moves a material batch into the warehouse from a distributed material.
    /// </summary>
    /// <param name="request">The request containing batch supply details.</param>
    /// <returns>Returns success or failure.</returns>
    [HttpPost("distribute-material/move-batch")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> MoveMaterialBatchToWarehouseFromDistribute(
        [FromBody] SupplyMaterialBatchFromHMaterialDistribute request
    )
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.MoveMaterialBatchToWarehouseFromDistribute(
            request,
            Guid.Parse(userId)
        );
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a list of reserved material quantities. If departmentId is passed you should expect only one item
    /// in the response
    /// </summary>
    /// <returns>Returns a paginated list of material departments.</returns>
    [HttpGet("{materialId}/reserved")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<ReservedMaterialReportDto>))]
    public async Task<IResult> GetMaterialReservedQuantity(
        [FromRoute] Guid materialId,
        [FromQuery] Guid? departmentId = null
    )
    {
        var result = await repository.GetReservedQuantitiesForMaterial(materialId, departmentId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Imports material stock from an Excel file.
    /// </summary>
    /// <param name="file">The uploaded Excel file containing materials.</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpPost("upload/stock")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> UploadMaterialStock(IFormFile file)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.ImportMaterialStockFromExcel(file);

        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Downloads material stock to an Excel file.
    /// </summary>
    /// <returns>Returns the Excel file containing material stock.</returns>
    [HttpGet("download/stock")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(byte[]))]
    public async Task<IResult> DownloadMaterialStock(
        [FromQuery] Guid? departmentId,
        [FromQuery] MaterialKind? kind
    )
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.ExportMaterialStockToExcel(
            Guid.Parse(userId),
            departmentId,
            kind
        );
        if (!result.IsSuccess)
            return result.ToProblemDetails();

        return TypedResults.File(
            result.Value,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "MaterialStock.xlsx"
        );
    }

    /// <summary>
    /// Gets the number of distinct material batches in a warehouse
    /// </summary>
    /// <param name="warehouseId">The warehouse Id.</param>
    /// <param name="materialId"></param>
    /// <param name="materialKind"></param>
    /// <param name="departmentId"></param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpGet("batches/distinct/{warehouseId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MaterialBatchCountDto))]
    public async Task<IResult> GetMaterialBatchCount(
        [FromRoute] Guid warehouseId,
        [FromQuery] Guid materialId,
        [FromQuery] MaterialKind materialKind,
        [FromQuery] Guid departmentId
    )
    {
        var result = await repository.GetMaterialBatchCount(
            warehouseId,
            materialId,
            materialKind,
            departmentId
        );
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of approved materials for a specific warehouse.
    /// </summary>
    /// <param name="kind">The kind of material needed.</param>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="searchQuery">Search query for filtering results.</param>
    /// <param name="departmentId">The department id for filtering results.</param>
    /// <returns>Returns a paginated list of materials with their batches and production departments.</returns>
    [HttpGet("batches/departments")]
    [AllowAnonymous]
    [ProducesResponseType(
        StatusCodes.Status200OK,
        Type = typeof(Paginateable<IEnumerable<MaterialBatchDepartmentDto>>)
    )]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetApprovedRawMaterialsByDepartment(
        [FromQuery] Guid? departmentId,
        [FromQuery] MaterialKind? kind,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null
    )
    {
        var result = await repository.GetMaterialsWithBatchesAndDepartments(
            page,
            pageSize,
            searchQuery,
            kind,
            departmentId
        );
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Gets a report of inconsistencies between batch aggregates and ledger records.
    /// </summary>
    [HttpGet("diagnostics/inconsistencies")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<BatchInconsistencyReport>))]
    public async Task<IResult> GetInconsistencyReport()
    {
        var result = await repository.GetInconsistencyReport();
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Resolves inconsistencies by syncing batch aggregates with ledger records.
    /// </summary>
    [HttpPost("maintenance/fix-inconsistencies")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IResult> ResolveInconsistencies()
    {
        var result = await repository.ResolveInconsistencies();
        return result.IsSuccess ? TypedResults.Ok() : result.ToProblemDetails();
    }
}
