using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.QcWorksheets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// Standard Test Procedures in the rebuilt QC module.
/// <para>
/// New and additive: this coexists with, and never touches, the live
/// <c>MaterialStandardTestProcedureController</c>/<c>ProductStandardTestProcedureController</c>
/// and their routes.
/// </para>
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/qc/worksheets/stps")]
[Authorize]
public class QcStandardTestProcedureController(IStandardTestProcedureRepository repository)
    : ControllerBase
{
    /// <summary>Retrieves a paginated list of standard test procedures.</summary>
    [HttpGet]
    [Authorize(QcWorksheetPermissionKeys.CanViewQcStps)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<StpSummaryDto>>))]
    public async Task<IResult> GetStps(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null,
        [FromQuery] QcDocumentStatus? status = null)
    {
        var result = await repository.GetStps(page, pageSize, searchQuery, status);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>Retrieves a standard test procedure, including its ordered steps.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(QcWorksheetPermissionKeys.CanViewQcStps)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(StpDetailDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetStp([FromRoute] Guid id)
    {
        var result = await repository.GetStp(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>Creates a new standard test procedure as Draft, version 1.</summary>
    [HttpPost]
    [Authorize(QcWorksheetPermissionKeys.CanCreateQcStp)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(StpDetailDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateStp([FromBody] CreateStpRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.CreateStp(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates a Draft or UnderReview standard test procedure. Editing while UnderReview
    /// resets it to Draft. An Effective document is rejected here — use create-new-version.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(QcWorksheetPermissionKeys.CanEditQcStp)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(StpDetailDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateStp([FromRoute] Guid id, [FromBody] UpdateStpRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.UpdateStp(id, request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Creates the next version as a new Draft. Valid only from Effective; the source
    /// record is untouched and stays in force.
    /// </summary>
    [HttpPost("{id:guid}/create-new-version")]
    [Authorize(QcWorksheetPermissionKeys.CanEditQcStp)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(StpDetailDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateNewVersion([FromRoute] Guid id)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.CreateNewVersion(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Moves Draft to UnderReview and puts the document into the approvers' pending queue
    /// via the existing approval engine.
    /// </summary>
    [HttpPost("{id:guid}/submit-for-review")]
    [Authorize(QcWorksheetPermissionKeys.CanEditQcStp)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(StpDetailDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> SubmitForReview([FromRoute] Guid id)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.SubmitForReview(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Approves with an electronic signature. Requires the caller's own password in
    /// addition to a valid session, then calls the same approval engine the generic
    /// approval endpoint uses.
    /// </summary>
    [HttpPost("{id:guid}/approve")]
    [Authorize(QcWorksheetPermissionKeys.CanApproveQcStp)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(StpDetailDto))]
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
    [Authorize(QcWorksheetPermissionKeys.CanApproveQcStp)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(StpDetailDto))]
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
    [Authorize(QcWorksheetPermissionKeys.CanApproveQcStp)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(StpDetailDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> MakeEffective([FromRoute] Guid id)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.MakeEffective(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Manual supersession outside the normal new-version flow, e.g. retiring a method
    /// entirely. Requires re-authentication and a reason for change.
    /// </summary>
    [HttpPost("{id:guid}/supersede")]
    [Authorize(QcWorksheetPermissionKeys.CanSupersedeQcStp)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(StpDetailDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> Supersede([FromRoute] Guid id, [FromBody] QcSupersedeRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.Supersede(id, request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Imports one or more legacy .docx standard test procedures as Drafts. Each file is
    /// reported independently: created draft, parse failure, or fields flagged for review.
    /// </summary>
    [HttpPost("import")]
    [Authorize(QcWorksheetPermissionKeys.CanImportQcStp)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<StpImportResultDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> Import()
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var files = HttpContext.Request.Form.Files;
        var result = await repository.Import(files, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}
