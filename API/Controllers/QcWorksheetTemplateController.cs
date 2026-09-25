using APP.Extensions;
using APP.IRepository;
using APP.Services.QcWorksheets.WorksheetDocxImport;
using APP.Utils;
using DOMAIN.Entities.QcWorksheets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SHARED;

namespace API.Controllers;

/// <summary>
/// Reusable worksheet templates in the rebuilt QC module. Same lifecycle and signature
/// rules as <see cref="QcStandardTestProcedureController"/>, carrying Sections -> Fields
/// instead of Steps. The <c>import</c> endpoint turns the lab's ARD Word worksheets into
/// reviewable proposals; unlike the STP import it writes nothing — saving a proposal goes
/// through <see cref="CreateTemplate"/>.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/qc/worksheets/templates")]
[Authorize]
public class QcWorksheetTemplateController(
    IWorksheetTemplateRepository repository,
    IWorksheetDocxImportService importService) : ControllerBase
{
    /// <summary>Retrieves a paginated list of worksheet templates.</summary>
    [HttpGet]
    [Authorize(QcWorksheetPermissionKeys.CanViewWorksheetTemplates)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<WorksheetTemplateSummaryDto>>))]
    public async Task<IResult> GetTemplates(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null,
        [FromQuery] QcDocumentStatus? status = null)
    {
        var result = await repository.GetTemplates(page, pageSize, searchQuery, status);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>Retrieves a worksheet template, including sections, fields and field revisions.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(QcWorksheetPermissionKeys.CanViewWorksheetTemplates)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WorksheetTemplateDetailDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetTemplate([FromRoute] Guid id)
    {
        var result = await repository.GetTemplate(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>Creates a new worksheet template as Draft, version 1.</summary>
    [HttpPost]
    [Authorize(QcWorksheetPermissionKeys.CanCreateWorksheetTemplate)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WorksheetTemplateDetailDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateTemplate([FromBody] CreateWorksheetTemplateRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.CreateTemplate(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates a Draft or UnderReview template. Editing while UnderReview resets it to
    /// Draft. An Effective template is rejected here — use create-new-version.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(QcWorksheetPermissionKeys.CanEditWorksheetTemplate)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WorksheetTemplateDetailDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateTemplate(
        [FromRoute] Guid id, [FromBody] UpdateWorksheetTemplateRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.UpdateTemplate(id, request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>Creates the next version as a new Draft. Valid only from Effective.</summary>
    [HttpPost("{id:guid}/create-new-version")]
    [Authorize(QcWorksheetPermissionKeys.CanEditWorksheetTemplate)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WorksheetTemplateDetailDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateNewVersion([FromRoute] Guid id)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.CreateNewVersion(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>Moves Draft to UnderReview and creates the approval stages.</summary>
    [HttpPost("{id:guid}/submit-for-review")]
    [Authorize(QcWorksheetPermissionKeys.CanEditWorksheetTemplate)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WorksheetTemplateDetailDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> SubmitForReview([FromRoute] Guid id)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.SubmitForReview(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>Approves with an electronic signature (password re-authentication required).</summary>
    [HttpPost("{id:guid}/approve")]
    [Authorize(QcWorksheetPermissionKeys.CanApproveWorksheetTemplate)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WorksheetTemplateDetailDto))]
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

    /// <summary>Rejects with an electronic signature, returning the template to Draft.</summary>
    [HttpPost("{id:guid}/reject")]
    [Authorize(QcWorksheetPermissionKeys.CanApproveWorksheetTemplate)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WorksheetTemplateDetailDto))]
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

    /// <summary>Brings an Approved template into force, retiring the version it supersedes.</summary>
    [HttpPost("{id:guid}/make-effective")]
    [Authorize(QcWorksheetPermissionKeys.CanApproveWorksheetTemplate)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WorksheetTemplateDetailDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> MakeEffective([FromRoute] Guid id)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.MakeEffective(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>Manual supersession. Requires re-authentication and a reason for change.</summary>
    [HttpPost("{id:guid}/supersede")]
    [Authorize(QcWorksheetPermissionKeys.CanSupersedeWorksheetTemplate)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WorksheetTemplateDetailDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> Supersede(
        [FromRoute] Guid id, [FromBody] QcSupersedeRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.Supersede(id, request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Proposes worksheet templates from one or more ARD .docx worksheets (multipart). Returns
    /// one proposal per file — a Draft template shape, SamplingPoint and Specification
    /// proposals, equipment/reagent matches, flags and per-field confidence — and <b>writes
    /// nothing</b>. Refused or unreadable files come back as a proposal carrying an
    /// <c>InvalidFile</c> flag, so one bad file never fails the batch.
    /// </summary>
    [HttpPost("import")]
    [Authorize(QcWorksheetPermissionKeys.CanImportQcWorksheetTemplates)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<WorksheetImportProposal>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> Import(CancellationToken cancellationToken)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        if (!HttpContext.Request.HasFormContentType || HttpContext.Request.Form.Files.Count == 0)
            return Result.Failure(WorksheetImportErrors.NoFiles).ToProblemDetails();

        var proposals = await importService.ProposeAsync(HttpContext.Request.Form.Files, cancellationToken);
        return TypedResults.Ok(proposals);
    }
}
