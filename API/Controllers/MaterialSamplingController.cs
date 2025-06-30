using APP.Extensions;
using APP.IRepository;
using DOMAIN.Entities.MaterialSampling;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/material-samplings")]
[Authorize]
public class MaterialSamplingController(IMaterialSamplingRepository repository) : ControllerBase
{
    /// <summary>
    /// Creates a sampling material
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateMaterialSampling([FromBody] CreateMaterialSamplingRequest request)
    {
        var result = await repository.CreateMaterialSampling(request);
        return result.IsSuccess ? TypedResults.Ok(result.Value): result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves the details of a sampling material by its ID.
    /// </summary>
    [HttpGet("{grnId:guid}/{batchId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MaterialSamplingDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetMaterialSamplingByMaterialId([FromRoute] Guid grnId, [FromRoute] Guid batchId)
    {
        var result = await repository.GetMaterialSamplingByGrnAndBatch(grnId, batchId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}