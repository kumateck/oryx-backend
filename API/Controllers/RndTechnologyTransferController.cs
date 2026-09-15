using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.RndTechnologyTransfers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/rnd/projects/{rndProjectId:guid}/technology-transfers")]
[Authorize]
public class RndTechnologyTransferController(
    IRndTechnologyTransferRepository repository,
    IApprovalRepository approvalRepository
) : ControllerBase
{
    /// <summary>
    /// Starts a new technology transfer for an approved formulation.
    /// </summary>
    [HttpPost]
    [Authorize(PermissionKeys.CanCreateRndTechnologyTransfer)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateTransfer(
        [FromRoute] Guid rndProjectId,
        [FromBody] CreateRndTechnologyTransferRequest request
    )
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.CreateTransfer(rndProjectId, request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Moves a technology transfer through DueDiligence to GapAnalysis to ProtocolApproved.
    /// </summary>
    [HttpPut("{transferId:guid}/status")]
    [Authorize(PermissionKeys.CanEditRndTechnologyTransfer)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateStatus(
        [FromRoute] Guid transferId,
        [FromBody] UpdateRndTechnologyTransferStatusRequest request
    )
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.UpdateStatus(transferId, request, Guid.Parse(userId));
        if (result.IsSuccess && request.Status == RndTechnologyTransferStatus.ProtocolInReview)
            await approvalRepository.CreateInitialApprovalsAsync(nameof(RndTechnologyTransfer), transferId);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Promotes an approved-protocol technology transfer into a real, versioned
    /// BillOfMaterial for the R&amp;D project's product, and stamps the product's
    /// master formula number and revision.
    /// </summary>
    [HttpPost("{transferId:guid}/promote")]
    [Authorize(PermissionKeys.CanPromoteRndTechnologyTransfer)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> PromoteToProduction([FromRoute] Guid transferId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.PromoteToProduction(transferId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a technology transfer by its ID.
    /// </summary>
    [HttpGet("{transferId:guid}")]
    [Authorize(PermissionKeys.CanViewRndTechnologyTransfers)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(RndTechnologyTransferDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetTransfer([FromRoute] Guid transferId)
    {
        var result = await repository.GetTransfer(transferId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves the paginated list of technology transfers for an R&amp;D project.
    /// </summary>
    [HttpGet]
    [Authorize(PermissionKeys.CanViewRndTechnologyTransfers)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<RndTechnologyTransferDto>>))]
    public async Task<IResult> GetTransfersForProject(
        [FromRoute] Guid rndProjectId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10
    )
    {
        var result = await repository.GetTransfersForProject(rndProjectId, page, pageSize);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}
