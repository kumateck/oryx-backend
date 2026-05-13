using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.ProductStandardTestProcedures;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/product-stps")]
[Authorize]
public class ProductStandardTestProcedureController(
    IProductStandardTestProcedureRepository repository
) : ControllerBase
{
    /// <summary>
    /// Creates a product standard test procedure
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateStandardTestProcedure(
        [FromBody] CreateProductStandardTestProcedureRequest request
    )
    {
        var result = await repository.CreateProductStandardTestProcedure(request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of standard test procedures.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(
        StatusCodes.Status200OK,
        Type = typeof(Paginateable<IEnumerable<ProductStandardTestProcedureDto>>)
    )]
    public async Task<IResult> GetStandardTestProcedures(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null
    )
    {
        var result = await repository.GetProductStandardTestProcedures(page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves the details of a specific standard test procedure by its ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProductStandardTestProcedureDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetStandardTestProcedure([FromRoute] Guid id)
    {
        var result = await repository.GetProductStandardTestProcedure(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves the details of a specific standard test procedure by its ID.
    /// </summary>
    [HttpGet("{productId:guid}/product")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProductStandardTestProcedureDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetStandardTestProcedureByProduct([FromRoute] Guid productId)
    {
        var result = await repository.GetProductStandardTestProcedureByProduct(productId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates the details of an existing standard test procedure.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<ProductStpMappingDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateStandardTestProcedure(
        [FromRoute] Guid id,
        [FromBody] UpdateProductStandardTestProcedureRequest request
    )
    {
        var result = await repository.UpdateProductStandardTestProcedure(id, request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a specific standard test procedure by its ID.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteStandardTestProcedure([FromRoute] Guid id)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.DeleteProductStandardTestProcedure(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of products that are not yet used in any standard test procedure.
    /// </summary>
    [HttpGet("unused-products")]
    [ProducesResponseType(
        StatusCodes.Status200OK,
        Type = typeof(Paginateable<IEnumerable<ProductListDto>>)
    )]
    public async Task<IResult> GetProductsNotUsedInStandardTestProcedure(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null
    )
    {
        var result = await repository.GetProductsNotUsedInStandardTestProcedure(
            page,
            pageSize,
            searchQuery
        );
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of standard test procedures that are not linked to any ARD.
    /// </summary>
    [HttpGet("unlinked-to-ard")]
    [ProducesResponseType(
        StatusCodes.Status200OK,
        Type = typeof(Paginateable<IEnumerable<ProductStandardTestProcedureDto>>)
    )]
    public async Task<IResult> GetProductStandardTestProceduresNotLinkedToArd(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null
    )
    {
        var result = await repository.GetProductStandardTestProceduresNotLinkedToArd(
            page,
            pageSize,
            searchQuery
        );
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}
