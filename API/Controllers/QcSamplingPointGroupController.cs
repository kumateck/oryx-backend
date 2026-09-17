using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.QcWorksheets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// Sampling point groups: the reference data a <c>SpecificationCharacteristic</c>'s
/// Alert/Action tier is grouped by, and the table Milestone 6's <c>MonitoringProgram</c> will
/// reference.
/// <para>
/// Plain CRUD with no lifecycle and no versioning — this is reference data, not a controlled
/// document — so every action sits behind the single
/// <see cref="QcWorksheetPermissionKeys.CanManageSamplingPointGroups"/> key rather than a
/// view/create/edit/approve/supersede split.
/// </para>
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/qc/worksheets/sampling-point-groups")]
[Authorize]
public class QcSamplingPointGroupController(ISamplingPointGroupRepository repository) : ControllerBase
{
    /// <summary>
    /// Lists every sampling point group. Deliberately unpaginated: this populates the
    /// dropdown a characteristic's group is chosen from, and a partial page would hide
    /// options an author needs.
    /// </summary>
    [HttpGet]
    [Authorize(QcWorksheetPermissionKeys.CanManageSamplingPointGroups)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<SamplingPointGroupDto>))]
    public async Task<IResult> GetSamplingPointGroups([FromQuery] string searchQuery = null)
    {
        var result = await repository.GetSamplingPointGroups(searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>Retrieves a single sampling point group.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(QcWorksheetPermissionKeys.CanManageSamplingPointGroups)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SamplingPointGroupDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetSamplingPointGroup([FromRoute] Guid id)
    {
        var result = await repository.GetSamplingPointGroup(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>Creates a sampling point group. Names are unique among live groups.</summary>
    [HttpPost]
    [Authorize(QcWorksheetPermissionKeys.CanManageSamplingPointGroups)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SamplingPointGroupDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> CreateSamplingPointGroup(
        [FromBody] CreateSamplingPointGroupRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.CreateSamplingPointGroup(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>Renames or re-describes a sampling point group.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(QcWorksheetPermissionKeys.CanManageSamplingPointGroups)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SamplingPointGroupDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateSamplingPointGroup(
        [FromRoute] Guid id, [FromBody] UpdateSamplingPointGroupRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.UpdateSamplingPointGroup(id, request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a sampling point group. Refused while any specification characteristic still
    /// groups its Alert/Action tier by it.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(QcWorksheetPermissionKeys.CanManageSamplingPointGroups)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteSamplingPointGroup([FromRoute] Guid id)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.DeleteSamplingPointGroup(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}
