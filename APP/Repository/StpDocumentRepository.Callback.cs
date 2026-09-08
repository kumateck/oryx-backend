using System.Security.Cryptography;
using System.Text.Json;
using APP.IRepository;
using APP.Services.OnlyOffice;
using APP.Services.Storage;
using AutoMapper;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using DOMAIN.Entities.StpDocuments;
using DOMAIN.Entities.MaterialStandardTestProcedures;
using DOMAIN.Entities.ProductStandardTestProcedures;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SHARED;

namespace APP.Repository;

public partial class StpDocumentRepository
{
    public async Task<Result> HandleSaveCallback(Guid documentId, string rawBody, string headerToken)
    {
        if (string.IsNullOrWhiteSpace(rawBody))
            return Result.Failure(StpDocumentErrors.CallbackUnauthorized);

        string bodyToken = null;
        try
        {
            using var rawDocument = JsonDocument.Parse(rawBody);
            if (rawDocument.RootElement.TryGetProperty("token", out var tokenProp) &&
                tokenProp.ValueKind == JsonValueKind.String)
            {
                bodyToken = tokenProp.GetString();
            }
        }
        catch (JsonException)
        {
            return Result.Failure(StpDocumentErrors.CallbackUnauthorized);
        }

        var verifyResult = onlyOfficeConfigService.VerifyCallbackToken(
            !string.IsNullOrWhiteSpace(headerToken) ? headerToken : bodyToken
        );
        if (verifyResult.IsFailure)
            return Result.Failure(verifyResult.Error);

        var payload = verifyResult.Value;
        if (!payload.TryGetProperty("status", out var statusProp) || statusProp.ValueKind != JsonValueKind.Number)
            return Result.Failure(StpDocumentErrors.CallbackUnauthorized);

        var status = statusProp.GetInt32();

        // 1 = user (dis)connecting from co-editing, 3/7 = save errors: nothing to persist.
        if (status is 1 or 3 or 7)
            return Result.Success();

        // 4 = closed with no changes: release the edit lock, nothing to persist.
        if (status == 4)
        {
            await ReleaseLock(documentId);
            return Result.Success();
        }

        // Only 2 (MustSave - editing session closed) and 6 (MustForceSave) carry a document.
        if (status != 2 && status != 6)
            return Result.Success();

        if (!payload.TryGetProperty("url", out var urlProp) || urlProp.ValueKind != JsonValueKind.String)
            return Result.Failure(StpDocumentErrors.CallbackPersistenceFailed);

        var downloadUrl = urlProp.GetString();

        var doc = await context.StpDocuments.Include(d => d.Versions).FirstOrDefaultAsync(d => d.Id == documentId);
        if (doc == null)
            return Result.Success();

        // Approved documents are opened view-only (permissions.edit = false in the signed
        // config), so ONLYOFFICE itself should never let this fire for one. Guard anyway:
        // starting a new draft is a deliberate action (UploadVersion/CreateBlank), not something
        // a stray callback should trigger.
        if (!StpDocumentWorkflowPolicy.CanTransition(doc.Status, StpDocumentStatus.InReview))
            return Result.Success();

        try
        {
            var trustedUrl = onlyOfficeConfigService.ValidateDownloadUrl(downloadUrl);
            if (trustedUrl.IsFailure)
            {
                logger.LogWarning("Rejected untrusted ONLYOFFICE callback URL for STP document {DocumentId}", documentId);
                return Result.Failure(trustedUrl.Error);
            }

            using var httpClient = httpClientFactory.CreateClient();
            using var response = await httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentLength > MaxFileSizeBytes)
                return Result.Failure(StpDocumentErrors.CallbackPersistenceFailed);
            var bytes = await response.Content.ReadAsByteArrayAsync();
            if (bytes.LongLength > MaxFileSizeBytes)
                return Result.Failure(StpDocumentErrors.CallbackPersistenceFailed);
            var newSha256 = ComputeSha256(bytes);

            var currentVersion = doc.CurrentDraftVersionId.HasValue
                ? doc.Versions.FirstOrDefault(v => v.Id == doc.CurrentDraftVersionId.Value)
                : null;

            if (currentVersion != null && currentVersion.Sha256 == newSha256)
            {
                if (status == 2)
                    await ReleaseLock(documentId);
                return Result.Success();
            }

            Guid? actingUserId = null;
            if (
                payload.TryGetProperty("actions", out var actionsProp) &&
                actionsProp.ValueKind == JsonValueKind.Array &&
                actionsProp.GetArrayLength() > 0 &&
                actionsProp[0].TryGetProperty("userid", out var useridProp) &&
                useridProp.ValueKind == JsonValueKind.String &&
                Guid.TryParse(useridProp.GetString(), out var parsedUserId)
            )
            {
                actingUserId = parsedUserId;
            }

            if (!actingUserId.HasValue || actingUserId != doc.LockedById)
            {
                logger.LogWarning(
                    "Rejected ONLYOFFICE callback actor for STP document {DocumentId}",
                    documentId
                );
                return Result.Failure(StpDocumentErrors.CallbackUnauthorized);
            }

            var nextVersionNumber = (doc.Versions.Count != 0 ? doc.Versions.Max(v => v.VersionNumber) : 0) + 1;
            var storageKey = $"{doc.Id}/v{nextVersionNumber}.docx";
            var fileName = currentVersion?.FileName ?? "Document.docx";

            var uploadFile = WrapAsFormFile(bytes, fileName, WordMimeType);
            var uploadResult = await blobStorageService.UploadBlobAsync(BucketName, uploadFile, storageKey);
            if (uploadResult.IsFailure)
            {
                logger.LogError("Unable to store ONLYOFFICE save for STP document {DocumentId}: {Error}", documentId, uploadResult.Error.Code);
                return Result.Failure(StpDocumentErrors.CallbackPersistenceFailed);
            }

            var newVersion = new StpDocumentVersion
            {
                StpDocumentId = doc.Id,
                VersionNumber = nextVersionNumber,
                StorageKey = storageKey,
                FileName = fileName,
                Sha256 = newSha256,
                Size = bytes.LongLength,
                Source = status == 6 ? StpDocumentVersionSource.ManualSave : StpDocumentVersionSource.AutoSave,
                CreatedById = actingUserId
            };
            context.StpDocumentVersions.Add(newVersion);
            await context.SaveChangesAsync();

            doc.CurrentDraftVersionId = newVersion.Id;
            if (status == 2)
            {
                doc.LockedById = null;
                doc.LockedAt = null;
            }
            context.StpDocuments.Update(doc);
            await context.SaveChangesAsync();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unable to process ONLYOFFICE save callback for STP document {DocumentId}", documentId);
            return Result.Failure(StpDocumentErrors.CallbackPersistenceFailed);
        }

        return Result.Success();
    }

    private async Task ReleaseLock(Guid documentId)
    {
        var doc = await context.StpDocuments.FirstOrDefaultAsync(d => d.Id == documentId);
        if (doc == null) return;

        doc.LockedById = null;
        doc.LockedAt = null;
        context.StpDocuments.Update(doc);
        await context.SaveChangesAsync();
    }
}
