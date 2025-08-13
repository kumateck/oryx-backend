using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.ProductSpecifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/product-specifications")]
[Authorize]
public class ProductSpecificationController(IProductSpecificationRepository repository) : ControllerBase
{

    /// <summary>
    /// Creates a product specification
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateProductSpecification(CreateProductSpecificationRequest request)
    {
        var result = await repository.CreateProductSpecification(request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of product specifications
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<ProductSpecificationDto>>))]
    public async Task<IResult> GetProductSpecifications([FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null)
    {
        var result = await repository.GetProductSpecifications(page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Retrieves the details of a product specification by its ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProductSpecificationDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetProductSpecification([FromRoute] Guid id)
    {
        var result = await repository.GetProductSpecification(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Retrieves the details of a product specification by its product ID.
    /// </summary>
    [HttpGet("product/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProductSpecificationDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetProductSpecificationByProductId([FromRoute] Guid id)
    {
        var result = await repository.GetProductSpecificationByProductId(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates a product specific by its ID.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent, Type = typeof(ProductSpecificationDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateProductSpecification([FromRoute] Guid id, [FromBody] CreateProductSpecificationRequest request)
    {
        var result = await repository.UpdateProductSpecification(id, request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a product specification by its ID.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteProductSpecification([FromRoute] Guid id)
    {
        var userId = (string)HttpContext.Items["Sub"];

        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.DeleteProductSpecification(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}