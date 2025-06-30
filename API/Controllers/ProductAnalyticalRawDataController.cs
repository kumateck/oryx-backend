using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.ProductAnalyticalRawData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;
[ApiController]
[Route("api/v{version:apiVersion}/product-ard")]
[Authorize]
public class ProductAnalyticalRawDataController(IProductAnalyticalRawDataRepository repository) : ControllerBase
{
    /// <summary>
    /// Creates product analytical raw data.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateAnalyticalRawData([FromBody] CreateProductAnalyticalRawDataRequest request)
    {
        var result = await repository.CreateAnalyticalRawData(request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of product analytical raw data based on search criteria.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<ProductAnalyticalRawDataDto>>))]
    public async Task<IResult> GetAnalyticalRawData([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchQuery = null)
    {
        var result = await repository.GetAnalyticalRawData(page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves specific product analytical raw data by its ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProductAnalyticalRawDataDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetAnalyticalRawData([FromRoute] Guid id)
    {
        var result = await repository.GetAnalyticalRawData(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Retrieves specific product analytical raw data by product Id.
    /// </summary>
    [HttpGet("product/{productId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<ProductAnalyticalRawDataDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetAnalyticalRawDataByProduct([FromRoute] Guid productId)
    {
        var result = await repository.GetAnalyticalRawDataByProduct(productId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Updates product analytical raw data by its ID.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent, Type = typeof(ProductAnalyticalRawDataDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateAnalyticalRawData([FromRoute] Guid id, [FromBody] CreateProductAnalyticalRawDataRequest request)
    {
        var result = await repository.UpdateAnalyticalRawData(id, request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes product analytical raw data by its ID.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteAnalyticRawData([FromRoute] Guid id)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.DeleteAnalyticalRawData(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Starts test for BMR
    /// </summary>
    [HttpPut("start-test/{batchManufacturingRecordId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> StartTestForMaterialBatch([FromRoute] Guid batchManufacturingRecordId)
    {
        var result = await repository.StartTestForBatchManufacturingRecord(batchManufacturingRecordId);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}