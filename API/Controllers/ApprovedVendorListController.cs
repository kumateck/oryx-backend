using APP.Extensions;
using APP.Services.ApprovedVendorLists;
using APP.Utils;
using DOMAIN.Entities.ApprovedVendorLists;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// QA's approved vendor (manufacturer) list for raw materials. Each revision is prepared, checked through the
/// document review and approved through the approval engine; the current approved revision restricts which
/// suppliers and manufacturers procurement may use for each listed material.
/// </summary>
[Route("api/v{version:apiVersion}/approved-vendor-list")]
[ApiController]
public class ApprovedVendorListController(ApprovedVendorListService lists) : ControllerBase
{
    private Guid? Actor => Guid.TryParse(HttpContext.Items["Sub"] as string, out var id) ? id : null;

    /// <summary>Every revision, newest first.</summary>
    [HttpGet]
    [Authorize(PermissionKeys.CanViewApprovedVendorList)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<ApprovedVendorListSummaryDto>))]
    public async Task<IResult> GetApprovedVendorLists() => TypedResults.Ok(await lists.List());

    /// <summary>One revision with its lines and Prepared / Checked / Approved by.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(PermissionKeys.CanViewApprovedVendorList)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApprovedVendorListDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetApprovedVendorList(Guid id)
    {
        var result = await lists.Get(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>One revision, for printing.</summary>
    [HttpGet("{id:guid}/print")]
    [Authorize(PermissionKeys.CanPrintApprovedVendorList)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApprovedVendorListDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetApprovedVendorListPrint(Guid id)
    {
        var result = await lists.Get(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>Raw materials, manufacturers and suppliers a line can name.</summary>
    [HttpGet("options")]
    [Authorize(PermissionKeys.CanViewApprovedVendorList)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApprovedVendorOptionsDto))]
    public async Task<IResult> GetApprovedVendorOptions() => TypedResults.Ok(await lists.Options());

    /// <summary>The manufacturers and suppliers already mapped to a raw material, to choose from on a line.</summary>
    [HttpGet("mappings/{materialId:guid}")]
    [Authorize(PermissionKeys.CanViewApprovedVendorList)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<ApprovedVendorMappingDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetApprovedVendorMappings(Guid materialId)
    {
        var result = await lists.Mappings(materialId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>Which suppliers and manufacturers the current approved list allows for the materials.</summary>
    [HttpPost("eligibility")]
    [Authorize(PermissionKeys.CanViewApprovedVendorEligibility)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<ApprovedVendorEligibilityDto>))]
    public async Task<IResult> GetApprovedVendorEligibility([FromBody] ApprovedVendorEligibilityRequest request) =>
        TypedResults.Ok(await lists.Eligibility(request));

    /// <summary>Prepares the first list. It goes to review, then approval.</summary>
    [HttpPost]
    [Authorize(PermissionKeys.CanCreateApprovedVendorList)]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> CreateApprovedVendorList([FromBody] SaveApprovedVendorListRequest request)
    {
        if (Actor is not { } actor) return TypedResults.Unauthorized();
        var result = await lists.Create(request, actor);
        return result.IsSuccess ? TypedResults.Created($"/api/v1/approved-vendor-list/{result.Value}", result.Value) : result.ToProblemDetails();
    }

    /// <summary>Prepares a new revision of the current approved list. It supersedes that list once approved.</summary>
    [HttpPost("revise")]
    [Authorize(PermissionKeys.CanReviseApprovedVendorList)]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> ReviseApprovedVendorList([FromBody] ReviseApprovedVendorListRequest request)
    {
        if (Actor is not { } actor) return TypedResults.Unauthorized();
        var result = await lists.Revise(request, actor);
        return result.IsSuccess ? TypedResults.Created($"/api/v1/approved-vendor-list/{result.Value}", result.Value) : result.ToProblemDetails();
    }

    /// <summary>Deletes a superseded revision from normal views, retaining its document history.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(PermissionKeys.CanDeleteApprovedVendorList)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> DeleteApprovedVendorList(Guid id)
    {
        if (Actor is not { } actor) return TypedResults.Unauthorized();
        var result = await lists.Delete(id, actor);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>Corrects a list while it is in review.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(PermissionKeys.CanEditApprovedVendorList)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> UpdateApprovedVendorList(Guid id, [FromBody] SaveApprovedVendorListRequest request)
    {
        if (Actor is not { } actor) return TypedResults.Unauthorized();
        var result = await lists.Update(id, request, actor);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}
