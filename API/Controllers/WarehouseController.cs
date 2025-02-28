using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.BinCards;
using DOMAIN.Entities.Checklists;
using DOMAIN.Entities.Grns;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Warehouses;
using DOMAIN.Entities.Warehouses.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Route("api/v{version:apiVersion}/warehouse")]
[ApiController]
public class WarehouseController(IWarehouseRepository repository) : ControllerBase
{
    #region Warehouse CRUD

    /// <summary>
    /// Creates a new warehouse.
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateWarehouse([FromBody] CreateWarehouseRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.CreateWarehouse(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves the details of a specific warehouse by its ID.
    /// </summary>
    [HttpGet("{warehouseId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WarehouseDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetWarehouse(Guid warehouseId)
    {
        var result = await repository.GetWarehouse(warehouseId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of warehouses based on search criteria.
    /// </summary>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<WarehouseDto>>))]
    public async Task<IResult> GetWarehouses([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchQuery = null)
    {
        var result = await repository.GetWarehouses(page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates the details of an existing warehouse.
    /// </summary>
    [HttpPut("{warehouseId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateWarehouse([FromBody] CreateWarehouseRequest request, Guid warehouseId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.UpdateWarehouse(request, warehouseId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a specific warehouse by its ID.
    /// </summary>
    [HttpDelete("{warehouseId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteWarehouse(Guid warehouseId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.DeleteWarehouse(warehouseId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    #endregion

    #region Warehouse Location CRUD

    /// <summary>
    /// Creates a new location within a warehouse.
    /// </summary>
    [HttpPost("{warehouseId}/location")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    public async Task<IResult> CreateWarehouseLocation([FromBody] CreateWarehouseLocationRequest request, Guid warehouseId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.CreateWarehouseLocation(request, warehouseId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves details of a specific warehouse location.
    /// </summary>
    [HttpGet("location/{locationId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WarehouseLocationDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetWarehouseLocation(Guid locationId)
    {
        var result = await repository.GetWarehouseLocation(locationId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of warehouse locations.
    /// </summary>
    [HttpGet("location")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<WarehouseLocationDto>>))]
    public async Task<IResult> GetWarehouseLocations([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchQuery = null)
    {
        var result = await repository.GetWarehouseLocations(page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates an existing warehouse location.
    /// </summary>
    [HttpPut("location/{locationId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateWarehouseLocation([FromBody] CreateWarehouseLocationRequest request, Guid locationId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.UpdateWarehouseLocation(request, locationId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a specific warehouse location.
    /// </summary>
    [HttpDelete("location/{locationId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteWarehouseLocation(Guid locationId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.DeleteWarehouseLocation(locationId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    #endregion

    #region Warehouse Location Rack CRUD

    /// <summary>
    /// Creates a new rack within a warehouse location.
    /// </summary>
    [HttpPost("{locationId}/rack")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    public async Task<IResult> CreateWarehouseLocationRack([FromBody] CreateWarehouseLocationRackRequest request, Guid locationId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.CreateWarehouseLocationRack(request, locationId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves details of a specific warehouse location rack.
    /// </summary>
    [HttpGet("rack/{rackId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WarehouseLocationRackDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetWarehouseLocationRack(Guid rackId)
    {
        var result = await repository.GetWarehouseLocationRack(rackId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of racks in warehouse locations.
    /// </summary>
    [HttpGet("rack")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<WarehouseLocationRackDto>>))]
    public async Task<IResult> GetWarehouseLocationRacks([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchQuery = null)
    {
        var result = await repository.GetWarehouseLocationRacks(page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates an existing warehouse location rack.
    /// </summary>
    [HttpPut("rack/{rackId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateWarehouseLocationRack([FromBody] CreateWarehouseLocationRackRequest request, Guid rackId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.UpdateWarehouseLocationRack(request, rackId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a specific warehouse location rack.
    /// </summary>
    [HttpDelete("rack/{rackId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteWarehouseLocationRack(Guid rackId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.DeleteWarehouseLocationRack(rackId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    #endregion

    #region Warehouse Location Shelf CRUD

    /// <summary>
    /// Creates a new shelf within a warehouse location rack.
    /// </summary>
    [HttpPost("{rackId}/shelf")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    public async Task<IResult> CreateWarehouseLocationShelf([FromBody] CreateWarehouseLocationShelfRequest request, Guid rackId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.CreateWarehouseLocationShelf(request, rackId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves details of a specific warehouse location shelf.
    /// </summary>
    [HttpGet("shelf/{shelfId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WarehouseLocationShelfDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetWarehouseLocationShelf(Guid shelfId)
    {
        var result = await repository.GetWarehouseLocationShelf(shelfId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of shelves in warehouse locations.
    /// </summary>
    [HttpGet("shelf")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<WarehouseLocationShelfDto>>))]
    public async Task<IResult> GetWarehouseLocationShelves([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchQuery = null)
    {
        var result = await repository.GetWarehouseLocationShelves(page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates an existing warehouse location shelf.
    /// </summary>
    [HttpPut("shelf/{shelfId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateWarehouseLocationShelf([FromBody] CreateWarehouseLocationShelfRequest request, Guid shelfId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.UpdateWarehouseLocationShelf(request, shelfId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a specific warehouse location shelf.
    /// </summary>
    [HttpDelete("shelf/{shelfId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteWarehouseLocationShelf(Guid shelfId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.DeleteWarehouseLocationShelf(shelfId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Retrieves a paginated list of shelves in a warehouse by material ID.
    /// </summary>
    [HttpGet("{warehouseId}/shelves/by-material/{materialId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<WarehouseLocationShelfDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> GetShelvesByMaterialId(Guid warehouseId, Guid materialId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchQuery = null)
    {
        var result = await repository.GetShelvesByMaterialId(page, pageSize, searchQuery, warehouseId, materialId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of shelves in a warehouse by material batch ID.
    /// </summary>
    [HttpGet("{warehouseId}/shelves/by-materialbatch/{materialBatchId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<WarehouseLocationShelfDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> GetShelvesByMaterialBatchId(Guid warehouseId, Guid materialBatchId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchQuery = null)
    {
        var result = await repository.GetShelvesByMaterialBatchId(page, pageSize, searchQuery, warehouseId, materialBatchId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of all shelves in a warehouse.
    /// </summary>
    [HttpGet("{warehouseId}/shelves")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<WarehouseLocationShelfDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> GetAllShelves(Guid warehouseId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchQuery = null)
    {
        var result = await repository.GetAllShelves(page, pageSize, searchQuery, warehouseId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    #endregion
    
    #region Arrival Location CRUD
    /// <summary>
    /// Retrieves the arrival location details of a specific warehouse by its ID.
    /// </summary>
    [HttpGet("{warehouseId}/arrival-location")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WarehouseArrivalLocationDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetArrivalLocationDetails(Guid warehouseId)
    {
        var result = await repository.GetArrivalLocationDetails(warehouseId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Retrieves a paginated list of distributed requisition materials for a specific warehouse.
    /// </summary>
    [HttpGet("{warehouseId}/distributed-requisition-materials")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<DistributedRequisitionMaterialDto>>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetDistributedRequisitionMaterials(Guid warehouseId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchQuery = null)
    {
        var result = await repository.GetDistributedRequisitionMaterials(warehouseId, page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Retrieves the details of a specific distributed requisition material by its ID.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("distributed-material/{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DistributedRequisitionMaterialDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetDistributedRequisitionMaterialById(Guid id)
    {
        var result = await repository.GetDistributedRequisitionMaterialById(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    
    /// <summary>
    /// Creates a new arrival location for a warehouse.
    /// </summary>
    [HttpPost("arrival-location")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> CreateArrivalLocation([FromBody] CreateArrivalLocationRequest request)
    {
        var result = await repository.CreateArrivalLocation(request);
        return result.IsSuccess ? TypedResults.Created($"/api/v1/warehouse/arrival-location/{result.Value}", result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Updates an existing arrival location.
    /// </summary>
    [HttpPut("arrival-location")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateArrivalLocation([FromBody] UpdateArrivalLocationRequest request)
    {
        var result = await repository.UpdateArrivalLocation(request);
        return result.IsSuccess ? TypedResults.Ok() : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Confirms the arrival of a distributed material.
    /// </summary>
    [HttpPost("confirm-arrival/{distributedMaterialId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> ConfirmArrival(Guid distributedMaterialId)
    {
        var result = await repository.ConfirmArrival(distributedMaterialId);
        return result.IsSuccess ? TypedResults.Ok() : result.ToProblemDetails();
    }
    #endregion
    
    #region Checklist CRUD

    /// <summary>
    /// Creates a new checklist.
    /// </summary>
    [HttpPost("checklist")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateChecklist([FromBody] CreateChecklistRequest request)
    {
        if (!ModelState.IsValid)
        {
            return TypedResults.BadRequest(ModelState);
        }
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.CreateChecklist(request,Guid.Parse(userId));

        return result.IsSuccess 
            ? TypedResults.Created($"/api/v1/warehouse/checklist/{result.Value}", result.Value) 
            : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves the details of a specific checklist by its ID.
    /// </summary>
    [HttpGet("checklist/{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ChecklistDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetChecklist(Guid id)
    {
        var result = await repository.GetChecklist(id);

        return result.IsSuccess 
            ? TypedResults.Ok(result.Value) 
            : result.ToProblemDetails();
    }
    
    #region MaterialBatch by DistributedRequisitionMaterial

    /// <summary>
    /// Retrieves the material batch details by distributed requisition material ID.
    /// </summary>
    [HttpGet("distributed-material/{distributedMaterialId}/material-batch")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<MaterialBatchDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetMaterialBatchByDistributedMaterial(Guid distributedMaterialId)
    {
        var result = await repository.GetMaterialBatchByDistributedMaterial(distributedMaterialId);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Retrieves the material batch details by distributed requisition material IDs.
    /// </summary>
    [HttpPost("distributed-material/material-batch")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<MaterialBatchDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetMaterialBatchByDistributedMaterials([FromBody]List<Guid> distributedMaterialIds)
    {
        var result = await repository.GetMaterialBatchByDistributedMaterials(distributedMaterialIds);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.ToProblemDetails();
    }

    #endregion
        
    #region GRN CRUD
    /// <summary>
    /// Creates a new GRN and assigns it to the specified material batches.
    /// </summary>
    [HttpPost("grn")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateGrn([FromBody] CreateGrnRequest request, [FromQuery] List<Guid> materialBatchIds)
    {
        if (!ModelState.IsValid)
        {
            return TypedResults.BadRequest(ModelState);
        }

        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.CreateGrn(request, materialBatchIds, Guid.Parse(userId));

        return result.IsSuccess
            ? TypedResults.Created($"/api/v1/warehouse/grn/{result.Value}", result.Value)
            : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Gets a GRN by its ID.
    /// </summary>
    [HttpGet("grn/{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(GrnDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetGrn(Guid id)
    {
        var result = await repository.GetGrn(id);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Retrieves a paginated list of GRNs based on search criteria.
    /// </summary>
    [HttpGet("grns")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<GrnDto>>))]
    public async Task<IResult> GetGrns([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchQuery = null)
    {
        var result = await repository.GetGrns(page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    #endregion

    #region BinCardInformation

    [HttpGet("bincardinformation/{materialId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<BinCardInformationDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> GetBinCardInformation([FromRoute] Guid materialId,[FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchQuery = null)
    {
        var result = await repository.GetBinCardInformation(page, pageSize, searchQuery,materialId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    #endregion
    

    #endregion
    
}
