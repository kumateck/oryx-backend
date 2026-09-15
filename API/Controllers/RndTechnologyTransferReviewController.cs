using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.RndTechnologyTransfers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/rnd/technology-transfer-reviews")]
[Authorize(PermissionKeys.CanViewRndTechnologyTransfers)]
public class RndTechnologyTransferReviewController(IRndTechnologyTransferRepository repository) : ControllerBase
{
    [HttpGet("{transferId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(RndTechnologyTransferDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetReviewItem([FromRoute] Guid transferId)
    {
        var result = await repository.GetTransfer(transferId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}
