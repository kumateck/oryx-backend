using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.QualityAudits;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/qa/quality-audits")]
[Authorize]
public class QualityAuditController(IQualityAuditRepository repository) : ControllerBase
{
    /// <summary>
    /// Creates a new quality audit in the Planned state.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateAudit([FromBody] CreateQualityAuditRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.CreateAudit(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates a quality audit's plan while it is still Planned.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateAudit([FromRoute] Guid id, [FromBody] UpdateQualityAuditRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.UpdateAudit(id, request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Starts a Planned audit, moving it to InProgress.
    /// </summary>
    [HttpPut("{id:guid}/start")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> StartAudit([FromRoute] Guid id)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.StartAudit(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Records a response to a checklist item (templated or ad-hoc) while conducting the audit.
    /// </summary>
    [HttpPost("{id:guid}/checklist-responses")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> RecordChecklistResponse(
        [FromRoute] Guid id,
        [FromBody] RecordChecklistResponseRequest request
    )
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.RecordChecklistResponse(id, request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Raises a finding on the audit, optionally traced back to a specific checklist response.
    /// </summary>
    [HttpPost("{id:guid}/findings")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> RaiseFinding([FromRoute] Guid id, [FromBody] RaiseFindingRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.RaiseFinding(id, request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Raises the corrective/preventive action (CAPA) for a finding.
    /// </summary>
    [HttpPost("findings/{findingId:guid}/corrective-action")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> RaiseCorrectiveAction(
        [FromRoute] Guid findingId,
        [FromBody] RaiseCorrectiveActionRequest request
    )
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.RaiseCorrectiveAction(findingId, request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates a corrective action's status (not usable to close it - see the effectiveness endpoint).
    /// </summary>
    [HttpPut("corrective-actions/{capaId:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateCorrectiveActionStatus(
        [FromRoute] Guid capaId,
        [FromBody] UpdateCorrectiveActionStatusRequest request
    )
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.UpdateCorrectiveActionStatus(capaId, request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Verifies whether a corrective action was effective, closing it (and its finding) if so.
    /// </summary>
    [HttpPut("corrective-actions/{capaId:guid}/verify-effectiveness")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> VerifyCorrectiveActionEffectiveness(
        [FromRoute] Guid capaId,
        [FromBody] VerifyCorrectiveActionEffectivenessRequest request
    )
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.VerifyCorrectiveActionEffectiveness(capaId, request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Submits an in-progress audit for closure. Blocked while corrective actions are open,
    /// unless the caller explicitly accepts and justifies the deferral.
    /// </summary>
    [HttpPut("{id:guid}/submit-for-closure")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> SubmitForClosure([FromRoute] Guid id, [FromBody] SubmitAuditForClosureRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.SubmitForClosure(id, request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// QA sign-off closing (or rejecting the closure of) an audit pending closure.
    /// </summary>
    [HttpPut("{id:guid}/close")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> CloseAudit([FromRoute] Guid id, [FromBody] CloseQualityAuditRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.CloseAudit(id, request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves details of a quality audit by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(QualityAuditDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetAudit([FromRoute] Guid id)
    {
        var result = await repository.GetAudit(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of quality audits.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<QualityAuditDto>>))]
    public async Task<IResult> GetAudits(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null,
        [FromQuery] AuditStatus? status = null,
        [FromQuery] AuditType? type = null
    )
    {
        var result = await repository.GetAudits(page, pageSize, searchQuery, status, type);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves the active audit checklist templates available to select when creating an audit.
    /// </summary>
    [HttpGet("checklist-templates")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<AuditChecklistTemplateDto>))]
    public async Task<IResult> GetChecklistTemplates()
    {
        var result = await repository.GetChecklistTemplates();
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Generates and downloads the audit report as a PDF.
    /// </summary>
    [HttpGet("{id:guid}/report")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GenerateAuditReportPdf([FromRoute] Guid id)
    {
        var result = await repository.GenerateAuditReportPdf(id);
        if (result.IsFailure) return result.ToProblemDetails();

        return TypedResults.File(result.Value, "application/pdf", $"audit-report-{id}.pdf");
    }
}
