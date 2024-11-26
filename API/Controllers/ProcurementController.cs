using APP.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.Procurement.Manufacturers;
using DOMAIN.Entities.Procurement.Suppliers;

namespace API.Controllers;

[Route("api/v{version:apiVersion}/procurement")]
[ApiController]
public class ProcurementController(IProcurementRepository repository) : ControllerBase
{
    // ************* Manufacturer Endpoints *************

    /// <summary>
    /// Creates a new manufacturer.
    /// </summary>
    /// <param name="request">The CreateManufacturerRequest object.</param>
    /// <returns>Returns the ID of the created manufacturer.</returns>
    [HttpPost("manufacturer")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateManufacturer([FromBody] CreateManufacturerRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.CreateManufacturer(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a manufacturer by its ID.
    /// </summary>
    /// <param name="manufacturerId">The ID of the manufacturer.</param>
    /// <returns>Returns the manufacturer details.</returns>
    [HttpGet("manufacturer/{manufacturerId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ManufacturerDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetManufacturer(Guid manufacturerId)
    {
        var result = await repository.GetManufacturer(manufacturerId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of manufacturers.
    /// </summary>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="searchQuery">Search query for filtering results.</param>
    /// <returns>Returns a paginated list of manufacturers.</returns>
    [HttpGet("manufacturer")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<ManufacturerDto>>))]
    public async Task<IResult> GetManufacturers([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchQuery = null)
    {
        var result = await repository.GetManufacturers(page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Retrieves a list of manufacturers by their material ID.
    /// </summary>
    /// <param name="materialId">The ID of the material.</param>
    /// <returns>Returns the supplier details.</returns>
    [HttpGet("manufacturer/material/{materialId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SupplierDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetManufacturerByMaterial(Guid materialId)
    {
        var result = await repository.GetManufacturersByMaterial(materialId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates a specific manufacturer by its ID.
    /// </summary>
    /// <param name="request">The CreateManufacturerRequest object.</param>
    /// <param name="manufacturerId">The ID of the manufacturer to update.</param>
    /// <returns>Returns success or failure.</returns>
    [HttpPut("manufacturer/{manufacturerId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateManufacturer([FromBody] CreateManufacturerRequest request, Guid manufacturerId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.UpdateManufacturer(request, manufacturerId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a specific manufacturer by its ID.
    /// </summary>
    /// <param name="manufacturerId">The ID of the manufacturer to delete.</param>
    /// <returns>Returns success or failure.</returns>
    [HttpDelete("manufacturer/{manufacturerId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteManufacturer(Guid manufacturerId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.DeleteManufacturer(manufacturerId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    // ************* Supplier Endpoints *************

    /// <summary>
    /// Creates a new supplier.
    /// </summary>
    /// <param name="request">The CreateSupplierRequest object.</param>
    /// <returns>Returns the ID of the created supplier.</returns>
    [HttpPost("supplier")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateSupplier([FromBody] CreateSupplierRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.CreateSupplier(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a supplier by its ID.
    /// </summary>
    /// <param name="supplierId">The ID of the supplier.</param>
    /// <returns>Returns the supplier details.</returns>
    [HttpGet("supplier/{supplierId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SupplierDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetSupplier(Guid supplierId)
    {
        var result = await repository.GetSupplier(supplierId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of suppliers.
    /// </summary>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="searchQuery">Search query for filtering results.</param>
    /// <returns>Returns a paginated list of suppliers.</returns>
    [HttpGet("supplier")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<SupplierDto>>))]
    public async Task<IResult> GetSuppliers([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchQuery = null)
    {
        var result = await repository.GetSuppliers(page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Retrieves a list of suppliers by their material ID.
    /// </summary>
    /// <param name="materialId">The ID of the material.</param>
    /// <returns>Returns the supplier details.</returns>
    [HttpGet("supplier/material/{materialId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SupplierDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetSupplierByMaterial(Guid materialId)
    {
        var result = await repository.GetSupplierByMaterial(materialId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates a specific supplier by its ID.
    /// </summary>
    /// <param name="request">The CreateSupplierRequest object.</param>
    /// <param name="supplierId">The ID of the supplier to update.</param>
    /// <returns>Returns success or failure.</returns>
    [HttpPut("supplier/{supplierId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateSupplier([FromBody] CreateSupplierRequest request, Guid supplierId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.UpdateSupplier(request, supplierId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a specific supplier by its ID.
    /// </summary>
    /// <param name="supplierId">The ID of the supplier to delete.</param>
    /// <returns>Returns success or failure.</returns>
    [HttpDelete("supplier/{supplierId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteSupplier(Guid supplierId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.DeleteSupplier(supplierId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}
