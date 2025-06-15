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
    
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateMaterialSampling([FromBody] CreateMaterialSamplingRequest request)
    {
        var result = await repository.CreateMaterialSampling(request);
        return result.IsSuccess ? TypedResults.Ok(result.Value): result.ToProblemDetails();
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MaterialSamplingDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetMaterialSamplingByMaterialId([FromRoute] Guid id)
    {
        var result = await repository.GetMaterialSamplingByMaterialId(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}