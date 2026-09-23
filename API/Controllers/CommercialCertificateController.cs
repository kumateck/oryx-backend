using APP.Extensions;
using APP.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/qc/commercial-certificates")]
[Authorize]
public class CommercialCertificateController(
    ICommercialCertificateRepository repository) : ControllerBase
{
    private Guid? ActorId => Guid.TryParse(
        HttpContext.Items["Sub"] as string, out var id) ? id : null;

    [HttpPost("material-batches/{materialBatchId:guid}")]
    public async Task<IResult> GenerateForMaterialBatch(
        [FromRoute] Guid materialBatchId)
    {
        if (ActorId is not Guid actor) return TypedResults.Unauthorized();
        var result = await repository.GenerateForMaterialBatch(materialBatchId, actor);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.ToProblemDetails();
    }

    [HttpPost("materials/{materialSamplingId:guid}")]
    public async Task<IResult> GenerateForMaterial(
        [FromRoute] Guid materialSamplingId)
    {
        if (ActorId is not Guid actor) return TypedResults.Unauthorized();
        var result = await repository.GenerateForMaterial(materialSamplingId, actor);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.ToProblemDetails();
    }

    [HttpPost("products/{analyticalTestRequestId:guid}")]
    public async Task<IResult> GenerateForProduct(
        [FromRoute] Guid analyticalTestRequestId)
    {
        if (ActorId is not Guid actor) return TypedResults.Unauthorized();
        var result = await repository.GenerateForProduct(
            analyticalTestRequestId, actor);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.ToProblemDetails();
    }

    [HttpGet("{id:guid}")]
    public async Task<IResult> Get([FromRoute] Guid id)
    {
        var result = await repository.Get(id);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.ToProblemDetails();
    }
}
