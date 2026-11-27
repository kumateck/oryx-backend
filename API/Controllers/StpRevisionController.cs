using API.Authorization;
using APP.Extensions;
using APP.Services.Authorization;
using APP.Services.StpRevisions;
using DOMAIN.Entities.StpRevisions;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// Structured STP revisions (raw material, packaging material and product STPs). Each revision is prepared as a draft,
/// checked through the document review and approved through the approval engine; the approved revision is the STP's
/// content of record. Every action needs the key for its task in the revision's kind (stored kind for an existing one).
/// </summary>
[Route("api/v{version:apiVersion}/stp-revisions")]
[ApiController]
public class StpRevisionController(StpRevisionService revisions) : ControllerBase
{
    private Guid? Actor => Guid.TryParse(HttpContext.Items["Sub"] as string, out var id) ? id : null;

    /// <summary>Every revision of a kind, or of one STP number, newest first.</summary>
    [HttpGet]
    [StpRevisionPermission(StpRevisionTask.View)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<StpRevisionSummaryDto>))]
    public async Task<IResult> GetStpRevisions([FromQuery] StpRevisionKind kind, [FromQuery] string stpNumber = null)
    {
        if (Actor is not { } actor) return TypedResults.Unauthorized();
        var result = await revisions.List(kind, stpNumber, actor);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>The STP numbers of a kind with their current and in-preparation revisions.</summary>
    [HttpGet("numbers")]
    [StpRevisionPermission(StpRevisionTask.View)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<StpNumberOverviewDto>))]
    public async Task<IResult> GetStpRevisionNumbers([FromQuery] StpRevisionKind kind)
    {
        if (Actor is not { } actor) return TypedResults.Unauthorized();
        var result = await revisions.Numbers(kind, actor);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>One revision with its content and Prepared / Checked / Approved by.</summary>
    [HttpGet("{id:guid}")]
    [StpRevisionPermission(StpRevisionTask.View)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(StpRevisionDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetStpRevision(Guid id)
    {
        if (Actor is not { } actor) return TypedResults.Unauthorized();
        var result = await revisions.Get(id, actor);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>Reads an uploaded Word STP (.docx) into a proposal with a confidence on every value. Nothing is saved.</summary>
    [HttpPost("import")]
    [StpRevisionPermission(StpRevisionTask.Import)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(StpImportProposalDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> ImportStpRevision([FromQuery] StpRevisionKind kind, IFormFile file)
    {
        if (Actor is not { } actor) return TypedResults.Unauthorized();
        var result = await revisions.Import(kind, file, actor);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>Reads the current version of the STP's Word document into a proposal. Nothing is saved.</summary>
    [HttpPost("import/document")]
    [StpRevisionPermission(StpRevisionTask.Import)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(StpImportProposalDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> ImportStpRevisionFromDocument([FromBody] ImportStpDocumentRequest request)
    {
        if (Actor is not { } actor) return TypedResults.Unauthorized();
        var result = await revisions.ImportDocument(request, actor);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>Starts the first revision of an STP number as a draft, blank or from an import proposal.</summary>
    [HttpPost]
    [StpRevisionPermission(StpRevisionTask.Create)]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> CreateStpRevision([FromBody] CreateStpRevisionRequest request)
    {
        if (Actor is not { } actor) return TypedResults.Unauthorized();
        var result = await revisions.Create(request, actor);
        return result.IsSuccess ? TypedResults.Created($"/api/v1/stp-revisions/{result.Value}", result.Value) : result.ToProblemDetails();
    }

    /// <summary>Starts a new revision of the approved revision as a draft, with a copy of its content.</summary>
    [HttpPost("revise")]
    [StpRevisionPermission(StpRevisionTask.Revise)]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> ReviseStpRevision([FromBody] ReviseStpRevisionRequest request)
    {
        if (Actor is not { } actor) return TypedResults.Unauthorized();
        var result = await revisions.Revise(request, actor);
        return result.IsSuccess ? TypedResults.Created($"/api/v1/stp-revisions/{result.Value}", result.Value) : result.ToProblemDetails();
    }

    /// <summary>Saves a draft's header and content. Needs the version last read.</summary>
    [HttpPut("{id:guid}/draft")]
    [StpRevisionPermission(StpRevisionTask.EditDraft)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> SaveStpRevisionDraft(Guid id, [FromBody] SaveStpRevisionDraftRequest request)
    {
        if (Actor is not { } actor) return TypedResults.Unauthorized();
        var result = await revisions.SaveDraft(id, request, actor);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>Submits a draft: it becomes the fixed version and goes for review, then approval.</summary>
    [HttpPost("{id:guid}/submit")]
    [StpRevisionPermission(StpRevisionTask.Submit)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> SubmitStpRevision(Guid id, [FromBody] SubmitStpRevisionRequest request)
    {
        if (Actor is not { } actor) return TypedResults.Unauthorized();
        var result = await revisions.Submit(id, request, actor);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>Corrects a revision while its review is awaiting review or returned.</summary>
    [HttpPut("{id:guid}")]
    [StpRevisionPermission(StpRevisionTask.Edit)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> UpdateStpRevision(Guid id, [FromBody] UpdateStpRevisionRequest request)
    {
        if (Actor is not { } actor) return TypedResults.Unauthorized();
        var result = await revisions.Update(id, request, actor);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>Deletes a revision from use, retaining its history. A draft needs a reason.</summary>
    [HttpDelete("{id:guid}")]
    [StpRevisionPermission(StpRevisionTask.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteStpRevision(Guid id, [FromQuery] string reason = null)
    {
        if (Actor is not { } actor) return TypedResults.Unauthorized();
        var result = await revisions.Delete(id, actor, reason);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}
