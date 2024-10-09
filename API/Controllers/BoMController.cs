using APP.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using APP.IRepository;
using DOMAIN.Entities.BillOfMaterials;
using DOMAIN.Entities.Products;

namespace API.Controllers;

[Route("api/v{version:apiVersion}/bom")] 
[ApiController] 
public class BillOfMaterialController(IBoMRepository repository) : ControllerBase 
{ 
    /// <summary>
    /// Creates a new Bill of Material.
    /// </summary>
    /// <param name="request">The CreateBillOfMaterialRequest object.</param>
    /// <returns>Returns the ID of the created Bill of Material.</returns>
    [HttpPost] 
    //[Authorize] 
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(Guid))] 
    [ProducesResponseType(StatusCodes.Status400BadRequest)] 
    public async Task<IResult> CreateBillOfMaterial([FromBody] CreateBillOfMaterialRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.CreateBillOfMaterial(request, Guid.Parse(userId)); 
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a specific Bill of Material by its ID.
    /// </summary>
    /// <param name="billOfMaterialId">The ID of the Bill of Material.</param>
    /// <returns>Returns the Bill of Material.</returns>
    [HttpGet("{billOfMaterialId}")]
    //[Authorize] 
    [ProducesResponseType(StatusCodes.Status200OK)] 
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetBillOfMaterial(Guid billOfMaterialId) 
    { 
        var result = await repository.GetBillOfMaterial(billOfMaterialId); 
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of Bill of Materials.
    /// </summary>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="searchQuery">Search query for filtering results.</param>
    /// <returns>Returns a paginated list of Bill of Materials.</returns>
    [HttpGet]
    //[Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)] 
    public async Task<IResult> GetBillOfMaterials([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchQuery = null) 
    { 
        var result = await repository.GetBillOfMaterials(page, pageSize, searchQuery); 
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates a specific Bill of Material.
    /// </summary>
    /// <param name="request">The CreateProductBillOfMaterialRequest object containing updated data.</param>
    /// <param name="billOfMaterialId">The ID of the Bill of Material to be updated.</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpPut("{billOfMaterialId}")]
    //[Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateBillOfMaterial([FromBody] CreateProductBillOfMaterialRequest request, Guid billOfMaterialId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.UpdateBillOfMaterial(request, billOfMaterialId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Updates a specific Bill of Material.
    /// </summary>
    /// <param name="billOfMaterialId">The ID of the Bill of Material to be updated.</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpPut("archive/{billOfMaterialId}")]
    //[Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> ArchiveBillOfMaterial([FromBody] Guid billOfMaterialId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.ArchiveBillOfMaterial(billOfMaterialId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a specific Bill of Material.
    /// </summary>
    /// <param name="billOfMaterialId">The ID of the Bill of Material to be deleted.</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpDelete("{billOfMaterialId}")]
    //[Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteBillOfMaterial(Guid billOfMaterialId) 
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.DeleteBillOfMaterial(billOfMaterialId, Guid.Parse(userId)); 
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}

