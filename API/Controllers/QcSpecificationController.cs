using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.QcWorksheets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// Specifications in the rebuilt QC module.
/// <para>
/// New and additive: this coexists with, and never touches, the live
/// <c>MaterialSpecificationController</c>/<c>ProductSpecificationController</c> and their
/// routes.
/// </para>
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/qc/worksheets/specifications")]
[Authorize]
public class QcSpecificationController(ISpecificationRepository repository) : ControllerBase
{
    /// <summary>Retrieves a paginated list of specifications.</summary>
    [HttpGet]
    [Authorize(QcWorksheetPermissionKeys.CanViewQcSpecifications)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<SpecificationSummaryDto>>))]
    public async Task<IResult> GetSpecifications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null,
        [FromQuery] QcDocumentStatus? status = null,
        [FromQuery] SpecificationAppliesTo? appliesTo = null)
    {
        var result = await repository.GetSpecifications(page, pageSize, searchQuery, status, appliesTo);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>Retrieves a specification, including its worksheet links and characteristics.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(QcWorksheetPermissionKeys.CanViewQcSpecifications)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SpecificationDetailDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetSpecification([FromRoute] Guid id)
    {
        var result = await repository.GetSpecification(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Every field on the current Effective version of each linked worksheet template, which
    /// is what the SourceFieldKey dropdown is populated from. All field types are returned;
    /// the client filters to the realistic candidates.
    /// </summary>
    [HttpGet("{id:guid}/available-fields")]
    [Authorize(QcWorksheetPermissionKeys.CanEditQcSpecification)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<SpecificationAvailableFieldDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetAvailableFields([FromRoute] Guid id)
    {
        var result = await repository.GetAvailableFields(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>Creates a new specification as Draft, version 1.</summary>
    [HttpPost]
    [Authorize(QcWorksheetPermissionKeys.CanCreateQcSpecification)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SpecificationDetailDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateSpecification([FromBody] CreateSpecificationRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.CreateSpecification(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates a Draft or UnderReview specification. Editing while UnderReview resets it to
    /// Draft. An Effective document is rejected here — use create-new-version.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(QcWorksheetPermissionKeys.CanEditQcSpecification)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SpecificationDetailDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateSpecification(
        [FromRoute] Guid id, [FromBody] UpdateSpecificationRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.UpdateSpecification(id, request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Creates the next version as a new Draft. Valid only from Effective; the source record
    /// is untouched and stays in force.
    /// </summary>
    [HttpPost("{id:guid}/create-new-version")]
    [Authorize(QcWorksheetPermissionKeys.CanEditQcSpecification)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SpecificationDetailDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateNewVersion([FromRoute] Guid id)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.CreateNewVersion(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Moves Draft to UnderReview and puts the document into the approvers' pending queue via
    /// the existing approval engine.
    /// </summary>
    [HttpPost("{id:guid}/submit-for-review")]
    [Authorize(QcWorksheetPermissionKeys.CanEditQcSpecification)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SpecificationDetailDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> SubmitForReview([FromRoute] Guid id)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.SubmitForReview(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Approves with an electronic signature. Requires the caller's own password in addition
    /// to a valid session, then calls the same approval engine the generic approval endpoint
    /// uses.
    /// </summary>
    [HttpPost("{id:guid}/approve")]
    [Authorize(QcWorksheetPermissionKeys.CanApproveQcSpecification)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SpecificationDetailDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IResult> Approve([FromRoute] Guid id, [FromBody] QcApprovalRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        var roleIds = (List<Guid>)HttpContext.Items["Roles"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.Approve(id, request, Guid.Parse(userId), roleIds ?? []);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>Rejects with an electronic signature, returning the document to Draft.</summary>
    [HttpPost("{id:guid}/reject")]
    [Authorize(QcWorksheetPermissionKeys.CanApproveQcSpecification)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SpecificationDetailDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IResult> Reject([FromRoute] Guid id, [FromBody] QcApprovalRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        var roleIds = (List<Guid>)HttpContext.Items["Roles"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.Reject(id, request, Guid.Parse(userId), roleIds ?? []);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Brings an Approved document into force. If it supersedes an earlier version, that
    /// version retires in the same transaction.
    /// </summary>
    [HttpPost("{id:guid}/make-effective")]
    [Authorize(QcWorksheetPermissionKeys.CanApproveQcSpecification)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SpecificationDetailDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> MakeEffective([FromRoute] Guid id)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.MakeEffective(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Manual supersession outside the normal new-version flow, e.g. retiring a
    /// specification entirely. Requires re-authentication and a reason for change.
    /// </summary>
    [HttpPost("{id:guid}/supersede")]
    [Authorize(QcWorksheetPermissionKeys.CanSupersedeQcSpecification)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SpecificationDetailDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> Supersede([FromRoute] Guid id, [FromBody] QcSupersedeRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.Supersede(id, request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}
