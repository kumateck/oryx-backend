using API.Authorization;
using APP.Extensions;
using APP.Services.Authorization;
using APP.Services.Forms.Microbial;
using DOMAIN.Entities.Forms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SHARED;

namespace API.Controllers;

/// <summary>Typed microbial limits on the result fields of a specification's microbial template.</summary>
[ApiController]
[Authorize]
[Route("api/v{version:apiVersion}")]
public sealed class SpecificationMicrobialLimitsController(SpecificationMicrobialLimitService service) : ControllerBase
{
    private Guid? Actor => Guid.TryParse(HttpContext.Items["Sub"]?.ToString(), out var actor) ? actor : null;

    [HttpGet("material-specifications/{id:guid}/microbial-limits", Name = "GetMaterialSpecificationMicrobialLimits")]
    [Tags("MaterialSpecification")]
    [QcDocumentPermission(QcDocument.MaterialSpecification, QcTask.ViewMicrobialLimits)]
    [ProducesResponseType(typeof(SpecificationMicrobialLimitsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetMaterial(Guid id) => Answer(await service.Get(SpecificationMicrobialLimitService.Subject.Material, id));

    [HttpPut("material-specifications/{id:guid}/microbial-limits", Name = "SaveMaterialSpecificationMicrobialLimits")]
    [Tags("MaterialSpecification")]
    [QcDocumentPermission(QcDocument.MaterialSpecification, QcTask.EditMicrobialLimits)]
    [ProducesResponseType(typeof(SpecificationMicrobialLimitsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IResult> SaveMaterial(Guid id, SaveMicrobialLimitsRequest request) => Save(SpecificationMicrobialLimitService.Subject.Material, id, request);

    [HttpGet("product-specifications/{id:guid}/microbial-limits", Name = "GetProductSpecificationMicrobialLimits")]
    [Tags("ProductSpecification")]
    [QcDocumentPermission(QcDocument.ProductSpecification, QcTask.ViewMicrobialLimits)]
    [ProducesResponseType(typeof(SpecificationMicrobialLimitsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetProduct(Guid id) => Answer(await service.Get(SpecificationMicrobialLimitService.Subject.Product, id));

    [HttpPut("product-specifications/{id:guid}/microbial-limits", Name = "SaveProductSpecificationMicrobialLimits")]
    [Tags("ProductSpecification")]
    [QcDocumentPermission(QcDocument.ProductSpecification, QcTask.EditMicrobialLimits)]
    [ProducesResponseType(typeof(SpecificationMicrobialLimitsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IResult> SaveProduct(Guid id, SaveMicrobialLimitsRequest request) => Save(SpecificationMicrobialLimitService.Subject.Product, id, request);

    private async Task<IResult> Save(SpecificationMicrobialLimitService.Subject subject, Guid id, SaveMicrobialLimitsRequest request)
    {
        if (Actor is not { } actor) return TypedResults.Unauthorized();
        try { return Answer(await service.Save(subject, id, request, actor)); }
        catch (PostgresException error) when (error.SqlState is "40001" or "40P01") { return Changed(); }
        catch (DbUpdateException error) when (error.InnerException is PostgresException { SqlState: "40001" or "40P01" }) { return Changed(); }
        catch (InvalidOperationException error) when (error.InnerException is DbUpdateException { InnerException: PostgresException { SqlState: "40001" or "40P01" } }) { return Changed(); }
    }

    private static IResult Answer(Result<SpecificationMicrobialLimitsDto> result) => result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();

    private static IResult Changed() => Result.Failure(Error.Conflict("MicrobialLimit.Changed", "The limits changed while saving. Reload and review the current limits.")).ToProblemDetails();
}
