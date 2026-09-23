using APP.Extensions;
using APP.IRepository;
using DOMAIN.Entities.QualityRoutines;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/qc/microbial-requirements")]
[Authorize]
public class MicrobialRequirementController(IMicrobialRequirementRepository repository)
    : ControllerBase
{
    [HttpGet]
    public async Task<IResult> List([FromQuery] Guid? materialId, [FromQuery] Guid? productId)
    {
        var result = await repository.List(materialId, productId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost]
    public async Task<IResult> Create([FromBody] CreateMicrobialRequirementRequest request)
    {
        var userId = HttpContext.Items["Sub"] as string;
        if (!Guid.TryParse(userId, out var actorId)) return TypedResults.Unauthorized();
        var result = await repository.Create(request, actorId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}
