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
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves the details of a sampling product by its ID.
    /// </summary>
    [HttpGet("{bmrId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProductSamplingDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetProductSamplingByProductId([FromRoute] Guid bmrId)
    {
        var result = await repository.GetProductSamplingByBmrId(bmrId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Adds an issue number to sampling product
    /// </summary>
    [HttpPut("{productSamplingId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> AddIssueNumberToMaterialSampling([FromRoute] Guid productSamplingId, 
        [FromQuery] string issueNumber)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await 
            repository.AddIssueNumberToProductSample(productSamplingId, issueNumber, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}