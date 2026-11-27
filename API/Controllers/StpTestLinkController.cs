using API.Authorization;
using APP.Extensions;
using APP.Services.StpRevisions;
using DOMAIN.Entities.StpRevisions;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// Which STP test each specification test is performed by. A link names an STP number of the specification's kind and
/// the stable key of a test in its approved revision. Reading needs the specification's view key; changing, its edit key.
/// </summary>
[Route("api/v{version:apiVersion}/stp-test-links")]
[ApiController]
public class StpTestLinkController(StpTestLinkService links) : ControllerBase
{
    private Guid? Actor => Guid.TryParse(HttpContext.Items["Sub"] as string, out var id) ? id : null;

    /// <summary>A material specification's tests, the STP test each cites, and the STP tests it may cite.</summary>
    [HttpGet("material-specification/{materialSpecificationId:guid}")]
    [StpTestLinkPermission(false)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(StpTestLinksDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetMaterialSpecificationStpTestLinks(Guid materialSpecificationId)
    {
        if (Actor is not { } actor) return TypedResults.Unauthorized();
        var result = await links.Get(false, materialSpecificationId, actor);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>A product specification's tests, the STP test each cites, and the STP tests it may cite.</summary>
    [HttpGet("product-specification/{productSpecificationId:guid}")]
    [StpTestLinkPermission(false)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(StpTestLinksDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetProductSpecificationStpTestLinks(Guid productSpecificationId)
    {
        if (Actor is not { } actor) return TypedResults.Unauthorized();
        var result = await links.Get(true, productSpecificationId, actor);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>Cites an STP test from a material specification test; both fields empty clears it.</summary>
    [HttpPut("material-specification/{materialSpecificationId:guid}/{formSectionId:guid}")]
    [StpTestLinkPermission(true)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> SaveMaterialSpecificationStpTestLink(Guid materialSpecificationId, Guid formSectionId, [FromBody] SaveStpTestLinkRequest request)
    {
        if (Actor is not { } actor) return TypedResults.Unauthorized();
        var result = await links.Save(false, materialSpecificationId, formSectionId, request, actor);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>Cites an STP test from a product specification test; both fields empty clears it.</summary>
    [HttpPut("product-specification/{productSpecificationId:guid}/{formSectionId:guid}")]
    [StpTestLinkPermission(true)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> SaveProductSpecificationStpTestLink(Guid productSpecificationId, Guid formSectionId, [FromBody] SaveStpTestLinkRequest request)
    {
        if (Actor is not { } actor) return TypedResults.Unauthorized();
        var result = await links.Save(true, productSpecificationId, formSectionId, request, actor);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}
