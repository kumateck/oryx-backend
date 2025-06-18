using APP.Extensions;
using APP.IRepository;
using DOMAIN.Entities.ProductsSampling;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/product-samplings")]
[Authorize]
public class ProductSamplingController(IProductSamplingRepository repository) : ControllerBase
{

    /// <summary>
    /// Creates a sampling product
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateProductSampling([FromBody] CreateProductSamplingRequest request)
    {
        var result = await repository.CreateProductSampling(request);
        return result.IsSuccess ? TypedResults.Ok(result.Value): result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves the details of a sampling product by its ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProductSamplingDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetProductSamplingByProductId([FromRoute] Guid id)
    {
        var result = await repository.GetProductSamplingByProductId(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}