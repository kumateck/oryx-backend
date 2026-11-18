using APP.Extensions;
using APP.Services.Production;
using APP.Utils;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Products.Production;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SHARED;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/unstart-reservations/{materialId:guid}")]
public sealed class UnstartReservationsController(UnstartReservationRepairService service) : ControllerBase
{
    [HttpGet("raw", Name = "PreviewRawUnstartReservations")]
    [Authorize(PermissionKeys.CanViewApprovedRawMaterials)]
    [ProducesResponseType(typeof(UnstartReservationPreviewDto), StatusCodes.Status200OK)]
    public Task<IResult> PreviewRaw(Guid materialId) => Preview(materialId, MaterialKind.Raw);

    [HttpGet("packaging", Name = "PreviewPackagingUnstartReservations")]
    [Authorize(PermissionKeys.CanViewApprovedPackagingMaterials)]
    [ProducesResponseType(typeof(UnstartReservationPreviewDto), StatusCodes.Status200OK)]
    public Task<IResult> PreviewPackaging(Guid materialId) => Preview(materialId, MaterialKind.Package);

    [HttpPost("raw", Name = "ReleaseRawUnstartReservations")]
    [Authorize(PermissionKeys.CanRepairUnstartRawMaterialReservations)]
    [ProducesResponseType(typeof(ReleaseUnstartReservationsResultDto), StatusCodes.Status200OK)]
    public Task<IResult> ReleaseRaw(Guid materialId, [FromBody] ReleaseUnstartReservationsRequest request) => Apply(materialId, MaterialKind.Raw, request);

    [HttpPost("packaging", Name = "ReleasePackagingUnstartReservations")]
    [Authorize(PermissionKeys.CanRepairUnstartPackagingMaterialReservations)]
    [ProducesResponseType(typeof(ReleaseUnstartReservationsResultDto), StatusCodes.Status200OK)]
    public Task<IResult> ReleasePackaging(Guid materialId, [FromBody] ReleaseUnstartReservationsRequest request) => Apply(materialId, MaterialKind.Package, request);

    private async Task<IResult> Preview(Guid materialId, MaterialKind kind)
    {
        var result = await service.Preview(materialId, kind);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private async Task<IResult> Apply(Guid materialId, MaterialKind kind, ReleaseUnstartReservationsRequest request)
    {
        try
        {
            var result = await service.Apply(materialId, kind, request);
            return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
        }
        catch (DbUpdateConcurrencyException) { return Changed(); }
        catch (DbUpdateException error) when (error.InnerException is PostgresException { SqlState: "40001" or "40P01" or "55P03" or "23505" or "23514" }) { return Changed(); }
        catch (PostgresException error) when (error.SqlState is "40001" or "40P01" or "55P03" or "23505" or "23514") { return Changed(); }
    }

    private static IResult Changed() => Result.Failure(Error.Conflict("UnstartReservations.Concurrent",
        "The production or stock evidence changed. Review a fresh preview before repairing.")).ToProblemDetails();
}
