using APP.Extensions;
using APP.IRepository;
using DOMAIN.Entities.StpDocuments;
using APP.Services.StpDocuments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// Standard Test Procedure (STP) document editing - upload/create-blank, in-browser ONLYOFFICE
/// editing with autosave versioning, and the Draft -> InReview -> Approved review lifecycle.
/// OwnerType is nameof(MaterialStandardTestProcedure) or nameof(ProductStandardTestProcedure);
/// OwnerId is that entity's Id.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/stp-documents")]
[Authorize]
public class StpDocumentController(
    IStpDocumentRepository repository,
    APP.Services.OnlyOffice.IOnlyOfficeConfigService onlyOfficeConfigService,
    IStpDocumentAccessService accessService
) : ControllerBase
{
    /// <summary>
    /// Retrieves the STP document (with version history) for the given owner, or null if
    /// no document has been created yet for it.
    /// </summary>
    [HttpGet("{ownerType}/{ownerId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(StpDocumentDto))]
    public async Task<IResult> GetDocument([FromRoute] string ownerType, [FromRoute] Guid ownerId)
    {
        if (!await accessService.CanAccessOwner(User, ownerType, false)) return TypedResults.Forbid();
        var result = await repository.GetOrNull(ownerType, ownerId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Uploads a new .docx version, creating the document on first use. If the document is
    /// currently Approved this starts a new draft and requires reasonForChange; the file may be
    /// omitted in that case to start editing from a clone of the current effective version.
    /// </summary>
    [HttpPost("{ownerType}/{ownerId:guid}/versions")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(StpDocumentDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> UploadVersion(
        [FromRoute] string ownerType,
        [FromRoute] Guid ownerId,
        IFormFile file,
        [FromForm] string reasonForChange
    )
    {
        if (!await accessService.CanAccessOwner(User, ownerType, true)) return TypedResults.Forbid();
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.UploadVersion(ownerType, ownerId, file, Guid.Parse(userId), reasonForChange);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>Creates a brand-new STP document seeded with a minimal blank .docx.</summary>
    [HttpPost("{ownerType}/{ownerId:guid}/new")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(StpDocumentDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateBlank([FromRoute] string ownerType, [FromRoute] Guid ownerId)
    {
        if (!await accessService.CanAccessOwner(User, ownerType, true)) return TypedResults.Forbid();
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.CreateBlank(ownerType, ownerId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>Streams a specific version's .docx to an authenticated application user.</summary>
    [HttpGet("documents/{documentId:guid}/versions/{versionId:guid}/file")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IFormFile))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IResult> GetVersionFile([FromRoute] Guid documentId, [FromRoute] Guid versionId)
    {
        if (!await accessService.CanAccessDocument(User, documentId, false)) return TypedResults.Forbid();
        var result = await repository.GetVersionFile(documentId, versionId);
        if (result.IsFailure)
            return TypedResults.NoContent();

        var (stream, contentType, name) = result.Value;
        stream.Seek(0, SeekOrigin.Begin);
        return TypedResults.File(stream, contentType, name);
    }

    /// <summary>Short-lived signed file route used only by the ONLYOFFICE server.</summary>
    [HttpGet("documents/{documentId:guid}/versions/{versionId:guid}/editor-file")]
    [AllowAnonymous]
    public async Task<IResult> GetEditorVersionFile(
        [FromRoute] Guid documentId,
        [FromRoute] Guid versionId,
        [FromQuery] string accessToken
    )
    {
        var access = onlyOfficeConfigService.VerifyFileAccessToken(accessToken, documentId, versionId);
        if (access.IsFailure)
            return TypedResults.Unauthorized();

        var result = await repository.GetVersionFile(documentId, versionId);
        if (result.IsFailure)
            return result.ToProblemDetails();

        var (stream, contentType, name) = result.Value;
        stream.Seek(0, SeekOrigin.Begin);
        return TypedResults.File(stream, contentType, name);
    }

    /// <summary>
    /// Validates the caller may open the document given its current status, acquires/refreshes
    /// the edit lock, and returns the signed ONLYOFFICE editor config for the frontend to pass
    /// straight into DocsAPI.DocEditor.
    /// </summary>
    [HttpPost("documents/{documentId:guid}/editor-sessions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> CreateEditorSession([FromRoute] Guid documentId)
    {
        if (!await accessService.CanOpenEditor(User, documentId)) return TypedResults.Forbid();
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.GetEditorSession(documentId, Guid.Parse(userId));
        return result.IsSuccess
            ? TypedResults.Ok(new { result.Value.Config, result.Value.DocumentServerUrl })
            : result.ToProblemDetails();
    }

    /// <summary>
    /// ONLYOFFICE's save callback - called server-to-server by the Document Server, not by an
    /// authenticated user, so this is anonymous and instead verifies ONLYOFFICE's own JWT.
    /// Returns {"error":0} only after successful handling; failures return a non-zero
    /// acknowledgement so ONLYOFFICE retains the pending save and retries it.
    /// </summary>
    [HttpPost("documents/{documentId:guid}/callback")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [AllowAnonymous]
    public async Task<IResult> Callback([FromRoute] Guid documentId, [FromQuery] string accessToken)
    {
        if (onlyOfficeConfigService.VerifyCallbackAccessToken(accessToken, documentId).IsFailure)
            return TypedResults.Ok(new { error = 1 });
        Request.EnableBuffering();
        Request.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(Request.Body, leaveOpen: true);
        var rawBody = await reader.ReadToEndAsync();
        Request.Body.Seek(0, SeekOrigin.Begin);

        var authHeader = Request.Headers.Authorization.FirstOrDefault();
        var headerToken =
            authHeader != null && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? authHeader["Bearer ".Length..]
                : authHeader;

        var result = await repository.HandleSaveCallback(documentId, rawBody, headerToken);

        return result.IsSuccess ? TypedResults.Ok(new { error = 0 }) : TypedResults.Ok(new { error = 1 });
    }

    /// <summary>Draft -&gt; InReview.</summary>
    [HttpPost("documents/{documentId:guid}/submit-review")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(StpDocumentDto))]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> SubmitForReview([FromRoute] Guid documentId)
    {
        if (!await accessService.CanAccessDocument(User, documentId, true)) return TypedResults.Forbid();
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.SubmitForReview(documentId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>InReview -&gt; Reviewed with a separate review e-signature.</summary>
    [HttpPost("documents/{documentId:guid}/review")]
    public async Task<IResult> Review(
        [FromRoute] Guid documentId,
        [FromBody] SubmitStpDocumentVersionRequest request
    )
    {
        if (!await accessService.CanAccessDocument(User, documentId, true)) return TypedResults.Forbid();
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.Review(documentId, Guid.Parse(userId), request.Meaning, request.Password);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>InReview -&gt; Approved. Re-verifies the caller's password and captures an
    /// e-signature (21 CFR 11.50/11.200).</summary>
    [HttpPost("documents/{documentId:guid}/approve")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(StpDocumentDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> Approve(
        [FromRoute] Guid documentId,
        [FromBody] SubmitStpDocumentVersionRequest request
    )
    {
        if (!await accessService.CanAccessDocument(User, documentId, true)) return TypedResults.Forbid();
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.Approve(documentId, Guid.Parse(userId), request.Meaning, request.Password);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>InReview -&gt; Draft. Re-verifies the caller's password and captures an
    /// e-signature (21 CFR 11.50/11.200).</summary>
    [HttpPost("documents/{documentId:guid}/reject")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(StpDocumentDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> Reject(
        [FromRoute] Guid documentId,
        [FromBody] SubmitStpDocumentVersionRequest request
    )
    {
        if (!await accessService.CanAccessDocument(User, documentId, true)) return TypedResults.Forbid();
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.Reject(documentId, Guid.Parse(userId), request.Meaning, request.Password);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}
