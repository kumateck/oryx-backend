using APP.Extensions;
using APP.Repository;
using APP.Utils;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Specifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/specification-print")]
[Tags("SpecificationPrint")]
public class SpecificationPrintController(SpecificationPrintRepository repository) : ControllerBase
{
    [HttpGet("raw-material/{id:guid}", Name = "GetRawMaterialSpecificationPrintHistory")]
    [Authorize(PermissionKeys.CanExportRawMaterialSpecification)]
    [ProducesResponseType<SpecificationPrintHistory>(StatusCodes.Status200OK)]
    public Task<IResult> RawMaterial(Guid id, CancellationToken cancellationToken) =>
        Material(id, MaterialKind.Raw, cancellationToken);

    [HttpGet("packaging-material/{id:guid}", Name = "GetPackagingMaterialSpecificationPrintHistory")]
    [Authorize(PermissionKeys.CanExportPackagingMaterialSpecification)]
    [ProducesResponseType<SpecificationPrintHistory>(StatusCodes.Status200OK)]
    public Task<IResult> PackagingMaterial(Guid id, CancellationToken cancellationToken) =>
        Material(id, MaterialKind.Package, cancellationToken);

    [HttpGet("product/{id:guid}", Name = "GetProductSpecificationPrintHistory")]
    [Authorize(PermissionKeys.CanExportProductSpecification)]
    [ProducesResponseType<SpecificationPrintHistory>(StatusCodes.Status200OK)]
    public async Task<IResult> Product(Guid id, CancellationToken cancellationToken)
    {
        var result = await repository.Product(id, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private async Task<IResult> Material(Guid id, MaterialKind kind, CancellationToken cancellationToken)
    {
        var result = await repository.Material(id, kind, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}
