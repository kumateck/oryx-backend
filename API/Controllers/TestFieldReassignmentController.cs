using APP.Extensions;
using APP.Services.Forms;
using APP.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SHARED;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/test-field-reassignments/{assignmentId:guid}")]
public sealed class TestFieldReassignmentController(TemplateRepairService service) : ControllerBase
{
    [HttpPost("product", Name = "ReassignProductTestFields")]
    [Authorize(PermissionKeys.CanReassignProductTest)]
    [ProducesResponseType(typeof(ReassignTestFieldsResultDto), StatusCodes.Status200OK)]
    public Task<IResult> Product(Guid assignmentId, ReassignTestFieldsRequest request) => Run(assignmentId, TemplateRepairTestKind.Product, request);

    [HttpPost("raw-material", Name = "ReassignRawMaterialTestFields")]
    [Authorize(PermissionKeys.CanReassignRawMaterialTest)]
    [ProducesResponseType(typeof(ReassignTestFieldsResultDto), StatusCodes.Status200OK)]
    public Task<IResult> RawMaterial(Guid assignmentId, ReassignTestFieldsRequest request) => Run(assignmentId, TemplateRepairTestKind.RawMaterial, request);

    [HttpPost("packaging-material", Name = "ReassignPackagingMaterialTestFields")]
    [Authorize(PermissionKeys.CanReassignPackagingMaterialTest)]
    [ProducesResponseType(typeof(ReassignTestFieldsResultDto), StatusCodes.Status200OK)]
    public Task<IResult> PackagingMaterial(Guid assignmentId, ReassignTestFieldsRequest request) => Run(assignmentId, TemplateRepairTestKind.PackagingMaterial, request);

    private async Task<IResult> Run(Guid assignmentId, TemplateRepairTestKind kind, ReassignTestFieldsRequest request)
    {
        try { var result = await service.ReassignFields(assignmentId, kind, request); return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails(); }
        catch (DbUpdateConcurrencyException) { return Changed(); }
        catch (DbUpdateException error) when (error.InnerException is PostgresException { SqlState: "40001" or "40P01" or "55P03" or "23505" }) { return Changed(); }
        catch (PostgresException error) when (error.SqlState is "40001" or "40P01" or "55P03" or "23505") { return Changed(); }
    }
    private static IResult Changed() => Result.Failure(Error.Conflict("TestReassignment.ConcurrentChange", "The test changed. Reload its assignments and review the transfer again.")).ToProblemDetails();
}
