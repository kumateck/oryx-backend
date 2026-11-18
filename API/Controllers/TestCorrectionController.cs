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
[Route("api/v{version:apiVersion}/test-corrections/{responseId:guid}")]
public sealed class TestCorrectionController(TestCorrectionService service) : ControllerBase
{
    [HttpGet("product", Name = "GetProductTestCorrectionHistory")]
    [Authorize(PermissionKeys.CanViewProductTestCorrectionHistory)]
    [ProducesResponseType(typeof(TestCorrectionStateDto), StatusCodes.Status200OK)]
    public async Task<IResult> ProductHistory(Guid responseId)
    {
        var result = await service.History(responseId, TemplateRepairTestKind.Product);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    [HttpPost("product", Name = "ReturnProductTestForCorrection")]
    [Authorize(PermissionKeys.CanReturnProductTestForCorrection)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public Task<IResult> ProductReturn(Guid responseId, ReturnTestForCorrectionRequest request) => Run(responseId, TemplateRepairTestKind.Product, request);
    [HttpGet("raw-material", Name = "GetRawMaterialTestCorrectionHistory")]
    [Authorize(PermissionKeys.CanViewRawMaterialTestCorrectionHistory)]
    [ProducesResponseType(typeof(TestCorrectionStateDto), StatusCodes.Status200OK)]
    public async Task<IResult> RawMaterialHistory(Guid responseId)
    {
        var result = await service.History(responseId, TemplateRepairTestKind.RawMaterial);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    [HttpPost("raw-material", Name = "ReturnRawMaterialTestForCorrection")]
    [Authorize(PermissionKeys.CanReturnRawMaterialTestForCorrection)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public Task<IResult> RawMaterialReturn(Guid responseId, ReturnTestForCorrectionRequest request) => Run(responseId, TemplateRepairTestKind.RawMaterial, request);
    [HttpGet("packaging-material", Name = "GetPackagingMaterialTestCorrectionHistory")]
    [Authorize(PermissionKeys.CanViewPackagingMaterialTestCorrectionHistory)]
    [ProducesResponseType(typeof(TestCorrectionStateDto), StatusCodes.Status200OK)]
    public async Task<IResult> PackagingMaterialHistory(Guid responseId)
    {
        var result = await service.History(responseId, TemplateRepairTestKind.PackagingMaterial);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    [HttpPost("packaging-material", Name = "ReturnPackagingMaterialTestForCorrection")]
    [Authorize(PermissionKeys.CanReturnPackagingMaterialTestForCorrection)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public Task<IResult> PackagingMaterialReturn(Guid responseId, ReturnTestForCorrectionRequest request) => Run(responseId, TemplateRepairTestKind.PackagingMaterial, request);
    private async Task<IResult> Run(Guid id, TemplateRepairTestKind kind, ReturnTestForCorrectionRequest request)
    {
        try { var result = await service.Return(id, kind, request); return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails(); }
        catch (DbUpdateConcurrencyException) { return Changed(); }
        catch (InvalidOperationException e) when (e.InnerException is DbUpdateException { InnerException: PostgresException { SqlState: "40001" or "40P01" or "55P03" or "23505" } }) { return Changed(); }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: "40001" or "40P01" or "55P03" or "23505" }) { return Changed(); }
        catch (PostgresException e) when (e.SqlState is "40001" or "40P01" or "55P03" or "23505") { return Changed(); }
    }
    private static IResult Changed() => Result.Failure(Error.Conflict("TestCorrection.ConcurrentChange", "The test changed. Reload it before returning the results.")).ToProblemDetails();
}
