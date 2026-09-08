using APP.Services.OnlyOffice;
using DOMAIN.Entities.StpDocuments;
using Microsoft.AspNetCore.Http;
using SHARED;

namespace APP.IRepository;

public interface IStpDocumentRepository
{
    /// <summary>
    /// Returns the STP document (with versions/signatures) for the given owner, or a
    /// successful result with a null value if none has been created yet - callers use this
    /// to decide between the "no document" state and the editor/dual-mode viewer.
    /// </summary>
    Task<Result<StpDocumentDto>> GetOrNull(string ownerType, Guid ownerId);

    /// <summary>
    /// Uploads a new version (creating the StpDocument on first use). If the document is
    /// currently Approved, this starts a new draft and requires reasonForChange; passing a
    /// null file in that case clones the current effective version's content instead of
    /// requiring a fresh upload.
    /// </summary>
    Task<Result<StpDocumentDto>> UploadVersion(
        string ownerType,
        Guid ownerId,
        IFormFile file,
        Guid userId,
        string reasonForChange
    );

    /// <summary>Creates a brand-new STP document seeded with a minimal blank .docx.</summary>
    Task<Result<StpDocumentDto>> CreateBlank(string ownerType, Guid ownerId, Guid userId);

    /// <summary>
    /// Validates the caller may open the document given its current status, acquires/refreshes
    /// the edit lock when opening for edit, and returns the signed ONLYOFFICE editor config.
    /// </summary>
    Task<Result<OnlyOfficeEditorConfigResult>> GetEditorSession(Guid documentId, Guid userId);

    /// <summary>
    /// Verifies and processes an ONLYOFFICE save callback. Persistence and authentication
    /// failures return a failed result so the controller asks ONLYOFFICE to retry.
    /// </summary>
    Task<Result> HandleSaveCallback(Guid documentId, string rawBody, string headerToken);

    Task<Result<StpDocumentDto>> SubmitForReview(Guid documentId, Guid userId);

    Task<Result<StpDocumentDto>> Review(Guid documentId, Guid userId, string meaning, string password);

    Task<Result<StpDocumentDto>> Approve(Guid documentId, Guid userId, string meaning, string password);

    Task<Result<StpDocumentDto>> Reject(Guid documentId, Guid userId, string meaning, string password);

    /// <summary>Streams the blob for a specific version - used for "download version" and as
    /// the URL ONLYOFFICE's own container fetches document.url from.</summary>
    Task<Result<(Stream Stream, string ContentType, string Name)>> GetVersionFile(
        Guid documentId,
        Guid versionId
    );
}
