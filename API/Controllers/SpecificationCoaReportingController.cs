using APP.Extensions;
using APP.Services.Forms.CoaReporting;
using APP.Utils;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.QualityRoutines;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SHARED;

namespace API.Controllers;

/// <summary>Which tests of a product specification's templates print on the certificate of analysis.</summary>
[ApiController]
[Route("api/v{version:apiVersion}")]
public sealed class SpecificationCoaReportingController(SpecificationCoaReportingService service) : ControllerBase
{
    private Guid? Actor => Guid.TryParse(HttpContext.Items["Sub"]?.ToString(), out var actor) ? actor : null;

    [HttpGet("product-specifications/{id:guid}/coa-reporting/{analysisType}", Name = "GetProductSpecificationCoaReporting")]
    [Tags("ProductSpecification")]
    [Authorize(PermissionKeys.CanViewProductCoaReportedTests)]
    [ProducesResponseType(typeof(SpecificationCoaReportingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> Get(Guid id, AnalysisType analysisType) => Answer(await service.Get(id, analysisType));

    [HttpPut("product-specifications/{id:guid}/coa-reporting/{analysisType}", Name = "SaveProductSpecificationCoaReporting")]
    [Tags("ProductSpecification")]
    [Authorize(PermissionKeys.CanEditProductCoaReportedTests)]
    [ProducesResponseType(typeof(SpecificationCoaReportingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> Save(Guid id, AnalysisType analysisType, SaveCoaReportingRequest request)
    {
        if (Actor is not { } actor) return TypedResults.Unauthorized();
        try { return Answer(await service.Save(id, analysisType, request, actor)); }
        catch (PostgresException error) when (error.SqlState is "40001" or "40P01") { return Changed(); }
        catch (DbUpdateException error) when (error.InnerException is PostgresException { SqlState: "40001" or "40P01" or "23505" }) { return Changed(); }
        catch (InvalidOperationException error) when (error.InnerException is DbUpdateException { InnerException: PostgresException { SqlState: "40001" or "40P01" } }) { return Changed(); }
    }

    private static IResult Answer(Result<SpecificationCoaReportingDto> result) => result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();

    private static IResult Changed() => Result.Failure(Error.Conflict("CoaReporting.Changed",
        "The reported tests changed while saving. Reload and review the current choices.")).ToProblemDetails();
}
