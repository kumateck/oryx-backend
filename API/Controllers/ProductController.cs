using APP.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using APP.IRepository;
using DOMAIN.Entities.Products;

namespace API.Controllers;

[Route("api/v{version:apiVersion}/products")]
[ApiController]
public class ProductController(IProductRepository repository) : ControllerBase
{
    /// <summary>
    /// Creates a new product.
    /// </summary>
    /// <param name="request">The CreateProductRequest object.</param>
    /// <returns>Returns the ID of the created product.</returns>
    [HttpPost]
    //[Authorize]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateProduct([FromBody] CreateProductRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.CreateProduct(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a specific product by its ID.
    /// </summary>
    /// <param name="productId">The ID of the product.</param>
    /// <returns>Returns the product details.</returns>
    [HttpGet("{productId}")]
    //[Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProductDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetProduct(Guid productId)
    {
        var result = await repository.GetProduct(productId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of products.
    /// </summary>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="searchQuery">Search query for filtering results.</param>
    /// <returns>Returns a paginated list of products.</returns>
    [HttpGet]
    //[Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IResult> GetProducts([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchQuery = null)
    {
        var result = await repository.GetProducts(page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates a specific product by its ID.
    /// </summary>
    /// <param name="request">The UpdateProductRequest object containing updated product data.</param>
    /// <param name="productId">The ID of the product to be updated.</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpPut("{productId}")]
    //[Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateProduct([FromBody] UpdateProductRequest request, Guid productId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.UpdateProduct(request, productId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a specific product by its ID.
    /// </summary>
    /// <param name="productId">The ID of the product to be deleted.</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpDelete("{productId}")]
    //[Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteProduct(Guid productId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.DeleteProduct(productId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}
