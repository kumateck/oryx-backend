using APP.Extensions;
using APP.Services.Production;
using APP.Utils;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Requisitions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SHARED;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/unstart-leftovers")]
public sealed class UnstartLeftoversController(UnstartLeftoverRepairService service) : ControllerBase
{
    [HttpGet("raw", Name = "PreviewRawUnstartLeftovers")]
    [Authorize(PermissionKeys.CanViewRawMaterialRequisitions)]
    [ProducesResponseType(typeof(UnstartLeftoverPreviewDto), StatusCodes.Status200OK)]
    public Task<IResult> PreviewRaw() => Preview(MaterialKind.Raw);

    [HttpGet("packaging", Name = "PreviewPackagingUnstartLeftovers")]
    [Authorize(PermissionKeys.CanViewPackagingMaterialRequisitions)]
    [ProducesResponseType(typeof(UnstartLeftoverPreviewDto), StatusCodes.Status200OK)]
    public Task<IResult> PreviewPackaging() => Preview(MaterialKind.Package);

    [HttpPost("raw", Name = "RepairRawUnstartLeftovers")]
    [Authorize(PermissionKeys.CanRepairUnstartRawMaterialRequisitions)]
    [ProducesResponseType(typeof(RepairUnstartLeftoversResultDto), StatusCodes.Status200OK)]
    public Task<IResult> RepairRaw([FromBody] RepairUnstartLeftoversRequest request) => Apply(MaterialKind.Raw, request);

    [HttpPost("packaging", Name = "RepairPackagingUnstartLeftovers")]
    [Authorize(PermissionKeys.CanRepairUnstartPackagingMaterialRequisitions)]
    [ProducesResponseType(typeof(RepairUnstartLeftoversResultDto), StatusCodes.Status200OK)]
    public Task<IResult> RepairPackaging([FromBody] RepairUnstartLeftoversRequest request) => Apply(MaterialKind.Package, request);

    private async Task<IResult> Preview(MaterialKind kind)
    {
        var result = await service.Preview(kind);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private async Task<IResult> Apply(MaterialKind kind, RepairUnstartLeftoversRequest request)
    {
        try
        {
            var result = await service.Apply(kind, request);
            return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
        }
        catch (DbUpdateConcurrencyException) { return Changed(); }
        catch (DbUpdateException error) when (error.InnerException is PostgresException { SqlState: "40001" or "40P01" or "55P03" or "23505" or "23514" }) { return Changed(); }
        catch (PostgresException error) when (error.SqlState is "40001" or "40P01" or "55P03" or "23505" or "23514") { return Changed(); }
    }

    private static IResult Changed() => Result.Failure(Error.Conflict("UnstartLeftovers.Concurrent",
        "The production or stock evidence changed. Review a fresh preview before repairing.")).ToProblemDetails();
}
