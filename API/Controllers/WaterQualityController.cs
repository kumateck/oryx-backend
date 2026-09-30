using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.QualityRoutines;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/qc/water-quality")]
[Authorize]
public class WaterQualityController(IWaterQualityRepository repository)
    : ControllerBase
{
    private Guid? ActorId => Guid.TryParse(
        HttpContext.Items["Sub"] as string, out var id) ? id : null;

    [HttpGet("periods")]
    public async Task<IResult> ListPeriods()
    {
        var result = await repository.ListPeriods();
        return result.IsSuccess ? TypedResults.Ok(result.Value) :
            result.ToProblemDetails();
    }

    [HttpGet("certificates")]
    [ProducesResponseType(StatusCodes.Status200OK,
        Type = typeof(Paginateable<IEnumerable<EligibleWaterCertificateDto>>))]
    public async Task<IResult> ListEligibleCertificates(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null)
    {
        var result = await repository.ListEligibleCertificates(
            page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) :
            result.ToProblemDetails();
    }

    [HttpPost("periods")]
    public async Task<IResult> ActivatePeriod(
        [FromBody] ActivateWaterQualityPeriodRequest request)
    {
        if (ActorId is not Guid actor) return TypedResults.Unauthorized();
        var result = await repository.ActivatePeriod(request, actor);
        return result.IsSuccess ? TypedResults.Ok(result.Value) :
            result.ToProblemDetails();
    }

    [HttpPost("uses")]
    public async Task<IResult> RecordUse([FromBody] RecordWaterUseRequest request)
    {
        if (ActorId is not Guid actor) return TypedResults.Unauthorized();
        var result = await repository.RecordUse(request, actor);
        return result.IsSuccess ? TypedResults.Ok(result.Value) :
            result.ToProblemDetails();
    }

    [HttpPost("periods/{id:guid}/hold")]
    public async Task<IResult> HoldPeriod([FromRoute] Guid id,
        [FromBody] HoldWaterQualityPeriodRequest request)
    {
        if (ActorId is not Guid actor) return TypedResults.Unauthorized();
        var result = await repository.HoldPeriod(id, request, actor);
        return result.IsSuccess ? TypedResults.Ok(result.Value) :
            result.ToProblemDetails();
    }
}
