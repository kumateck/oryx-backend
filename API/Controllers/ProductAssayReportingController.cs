using APP.Extensions;
using APP.Services.Forms;
using APP.Utils;
using DOMAIN.Entities.ProductSpecifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SHARED;

namespace API.Controllers;

[ApiController]
[Tags("ProductSpecification")]
[Route("api/v{version:apiVersion}/product-specifications/{id:guid}/assay-reporting")]
public sealed class ProductAssayReportingController(ProductAssayReportingService service) : ControllerBase
{
    private Guid? Actor => Guid.TryParse(HttpContext.Items["Sub"]?.ToString(), out var actor) ? actor : null;

    [HttpGet(Name = "GetProductAssayReporting")]
    [Authorize(PermissionKeys.CanViewProductAssayReporting)]
    [ProducesResponseType(typeof(AssayReportingConfigurationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> Get(Guid id)
    {
        if (Actor is not { } actor) return TypedResults.Unauthorized();
        var result = await service.Get(id, actor);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPut(Name = "SaveProductAssayReporting")]
    [Authorize(PermissionKeys.CanEditProductAssayReporting)]
    [ProducesResponseType(typeof(AssayReportingConfigurationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> Save(Guid id, SaveAssayReportingRequest request)
    {
        if (Actor is not { } actor) return TypedResults.Unauthorized();
        try
        {
            var result = await service.Save(id, request, actor);
            return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
        }
        catch (DbUpdateConcurrencyException) { return Changed(); }
        catch (PostgresException error) when (error.SqlState is "40001" or "40P01" or "23505") { return Changed(); }
        catch (DbUpdateException error) when (error.InnerException is PostgresException { SqlState: "40001" or "40P01" or "23505" }) { return Changed(); }
        catch (InvalidOperationException error) when (error.InnerException is DbUpdateException { InnerException: PostgresException { SqlState: "40001" or "40P01" or "23505" } }) { return Changed(); }
    }

    private static IResult Changed() => Result.Failure(Error.Conflict("AssayReporting.Changed", "The reporting settings changed while saving. Reload and review the current settings.")).ToProblemDetails();
}
