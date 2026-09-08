using SHARED;

namespace DOMAIN.Entities.StpDocuments;

public static class StpDocumentErrors
{
    public static Error NotFound(Guid documentId) =>
        Error.NotFound("StpDocument.NotFound", $"The STP document with the Id: {documentId} was not found");

    public static Error VersionNotFound(Guid versionId) =>
        Error.NotFound("StpDocument.VersionNotFound", $"The STP document version with the Id: {versionId} was not found");

    public static Error FileRequired =>
        Error.Validation("StpDocument.FileRequired", "A .docx file is required");

    public static Error EmptyFile =>
        Error.Validation("StpDocument.EmptyFile", "The uploaded file is empty");

    public static Error InvalidFileType =>
        Error.Validation("StpDocument.InvalidFileType", "Only .docx files are supported. Macro-enabled documents (.docm) are not allowed");

    public static Error InvalidFileSignature =>
        Error.Validation("StpDocument.InvalidFileSignature", "The uploaded file is not a valid .docx document");

    public static Error FileTooLarge(long maxBytes) =>
        Error.Validation("StpDocument.FileTooLarge", $"The uploaded file exceeds the maximum allowed size of {maxBytes / (1024 * 1024)}MB");

    public static Error ReasonForChangeRequired =>
        Error.Validation("StpDocument.ReasonForChangeRequired", "A reason for change is required when starting a new draft of an approved document");

    public static Error MeaningRequired =>
        Error.Validation("StpDocument.MeaningRequired", "A signature meaning is required");

    public static Error InvalidOwnerType =>
        Error.Validation("StpDocument.InvalidOwnerType", "The STP owner type is not supported");

    public static Error OwnerNotFound(Guid ownerId) =>
        Error.NotFound("StpDocument.OwnerNotFound", $"The STP owner with the Id: {ownerId} was not found");

    public static Error SegregationOfDuties =>
        Error.Conflict(
            "StpDocument.SegregationOfDuties",
            "The author, reviewer, and approver must be different users"
        );

    public static Error ReviewRequired =>
        Error.Conflict("StpDocument.ReviewRequired", "A separate reviewer signature is required before approval");

    public static Error VersionChangeNotAllowed(StpDocumentStatus status) =>
        Error.Conflict(
            "StpDocument.VersionChangeNotAllowed",
            $"A new version cannot be created while the document is {status}"
        );

    public static Error EditorSessionActive =>
        Error.Conflict(
            "StpDocument.EditorSessionActive",
            "Close the document editor and allow its final save to complete before submitting for review"
        );

    public static Error NoDraftVersion =>
        Error.Conflict("StpDocument.NoDraftVersion", "This document has no current draft version");

    public static Error NoEffectiveVersion =>
        Error.Conflict("StpDocument.NoEffectiveVersion", "This document has no approved effective version");

    public static Error AlreadyExists =>
        Error.Conflict("StpDocument.AlreadyExists", "An STP document already exists for this owner. Use the upload/new-draft actions instead");

    public static Error InvalidTransition(StpDocumentStatus from, StpDocumentStatus to) =>
        Error.Conflict("StpDocument.InvalidTransition", $"Cannot move the document from {from} to {to}");

    public static Error LockedByAnotherUser(string lockedByName) =>
        Error.Conflict("StpDocument.Locked", $"This document is currently being edited by {lockedByName ?? "another user"}. Try again shortly");

    public static Error OnlyOfficeNotConfigured =>
        Error.Failure("StpDocument.OnlyOfficeNotConfigured", "The document editor is not configured (ONLYOFFICE_JWT_SECRET missing)");

    public static Error CallbackUnauthorized =>
        Error.Validation("StpDocument.CallbackUnauthorized", "Unable to verify the document editor callback signature");

    public static Error InvalidDocumentDownload =>
        Error.Validation("StpDocument.InvalidDocumentDownload", "The document download link is invalid or expired");

    public static Error StoredFileUnavailable =>
        Error.Failure(
            "StpDocument.StoredFileUnavailable",
            "The stored document file is unavailable. Replace it from Edit STP before opening the editor"
        );

    public static Error InvalidEditorDownloadUrl =>
        Error.Validation("StpDocument.InvalidEditorDownloadUrl", "The document editor returned an untrusted download URL");

    public static Error CallbackPersistenceFailed =>
        Error.Failure(
            "StpDocument.CallbackPersistenceFailed",
            "The document editor save could not be persisted; the editor must retry the callback"
        );
}
