using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.RndFormulations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/rnd/formulation-reviews")]
[Authorize(PermissionKeys.CanViewRndFormulations)]
public class RndFormulationReviewController(IRndFormulationRepository repository) : ControllerBase
{
    [HttpGet("{formulationId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(RndFormulationReviewDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetReviewItem([FromRoute] Guid formulationId)
    {
        var result = await repository.GetReviewItem(formulationId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet]
    [ProducesResponseType(
        StatusCodes.Status200OK,
        Type = typeof(Paginateable<IEnumerable<RndFormulationReviewDto>>)
    )]
    public async Task<IResult> GetReviewQueue(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50
    )
    {
        var result = await repository.GetReviewQueue(page, pageSize);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}
