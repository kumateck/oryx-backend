using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.MaterialAnalyticalRawData;
using DOMAIN.Entities.Materials;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/material-ard")]
[Authorize]
public class MaterialAnalyticalRawDataController(IMaterialAnalyticalRawDataRepository repository) : ControllerBase
{

    /// <summary>
    /// Creates analytical raw data.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateAnalyticalRawData([FromBody] CreateMaterialAnalyticalRawDataRequest request)
    {
        var result = await repository.CreateAnalyticalRawData(request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of analytical raw data based on search criteria.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<MaterialAnalyticalRawDataDto>>))]
    public async Task<IResult> GetAnalyticalRawData( [FromQuery] MaterialKind materialKind, [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null)
    {
        var result = await repository.GetAnalyticalRawData(page, pageSize, searchQuery, materialKind);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves specific analytical raw data by is ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MaterialAnalyticalRawDataDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetAnalyticalRawData([FromRoute] Guid id)
    {
        var result = await repository.GetAnalyticalRawData(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Retrieves specific analytical raw data by is material ID.
    /// </summary>
    [HttpGet("material/{materialId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MaterialAnalyticalRawDataDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetAnalyticalRawDataByMaterial([FromRoute] Guid materialId)
    {
        var result = await repository.GetAnalyticalRawDataByMaterial(materialId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Retrieves specific analytical raw data by is material batch ID.
    /// </summary>
    [HttpGet("material/batch/{materialBatchId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MaterialAnalyticalRawDataDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetAnalyticalRawDataByMaterialBatch([FromRoute] Guid materialBatchId)
    {
        var result = await repository.GetAnalyticalRawDataByMaterialBatch(materialBatchId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Updates analytical raw data by its ID.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent, Type = typeof(MaterialAnalyticalRawDataDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateAnalyticalRawData([FromRoute] Guid id, [FromBody] CreateMaterialAnalyticalRawDataRequest request)
    {
        var result = await repository.UpdateAnalyticalRawData(id, request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes analytical raw data by its ID.
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
    /// Starts test for material batch
    /// </summary>
    [HttpPut("start-test/{materialBatchId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> StartTestForMaterialBatch([FromRoute] Guid materialBatchId)
    {
        var result = await repository.StartTestForMaterialBatch(materialBatchId);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}