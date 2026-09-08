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
    public async Task<Result<OnlyOfficeEditorConfigResult>> GetEditorSession(Guid documentId, Guid userId)
    {
        var doc = await context
            .StpDocuments.Include(d => d.LockedBy)
            .FirstOrDefaultAsync(d => d.Id == documentId);

        if (doc == null)
            return Result.Failure<OnlyOfficeEditorConfigResult>(StpDocumentErrors.NotFound(documentId));

        Guid targetVersionId;
        bool edit;
        bool review;
        string mode;
        var acquireDraftLock = false;

        switch (doc.Status)
        {
            case StpDocumentStatus.Draft:
            {
                if (doc.CurrentDraftVersionId == null)
                    return Result.Failure<OnlyOfficeEditorConfigResult>(StpDocumentErrors.NoDraftVersion);

                var now = DateTime.UtcNow;
                var lockExpired = doc.LockedAt == null || now - doc.LockedAt.Value > LockTtl;
                if (doc.LockedById.HasValue && doc.LockedById != userId && !lockExpired)
                {
                    var lockedByName = doc.LockedBy != null ? $"{doc.LockedBy.FirstName} {doc.LockedBy.LastName}" : null;
                    return Result.Failure<OnlyOfficeEditorConfigResult>(StpDocumentErrors.LockedByAnotherUser(lockedByName));
                }

                targetVersionId = doc.CurrentDraftVersionId.Value;
                edit = true;
                review = false;
                mode = "edit";
                acquireDraftLock = true;
                break;
            }
            case StpDocumentStatus.InReview:
            case StpDocumentStatus.Reviewed:
            {
                if (doc.CurrentDraftVersionId == null)
                    return Result.Failure<OnlyOfficeEditorConfigResult>(StpDocumentErrors.NoDraftVersion);

                targetVersionId = doc.CurrentDraftVersionId.Value;
                edit = false;
                review = false;
                mode = "view";
                break;
            }
            case StpDocumentStatus.Approved:
            {
                if (doc.EffectiveVersionId == null)
                    return Result.Failure<OnlyOfficeEditorConfigResult>(StpDocumentErrors.NoEffectiveVersion);

                targetVersionId = doc.EffectiveVersionId.Value;
                edit = false;
                review = false;
                mode = "view";
                break;
            }
            default:
                return Result.Failure<OnlyOfficeEditorConfigResult>(StpDocumentErrors.NoDraftVersion);
        }

        var version = await context.StpDocumentVersions.FirstOrDefaultAsync(v => v.Id == targetVersionId);
        if (version == null)
            return Result.Failure<OnlyOfficeEditorConfigResult>(StpDocumentErrors.VersionNotFound(targetVersionId));

        var blobExists = await blobStorageService.BlobExistsAsync(BucketName, version.StorageKey);
        if (blobExists.IsFailure)
            return Result.Failure<OnlyOfficeEditorConfigResult>(blobExists.Error);
        if (!blobExists.Value)
            return Result.Failure<OnlyOfficeEditorConfigResult>(StpDocumentErrors.StoredFileUnavailable);

        if (acquireDraftLock)
        {
            doc.LockedById = userId;
            doc.LockedAt = DateTime.UtcNow;
            context.StpDocuments.Update(doc);
            await context.SaveChangesAsync();
        }

        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        var userName = user != null ? $"{user.FirstName} {user.LastName}" : userId.ToString();

        var config = onlyOfficeConfigService.BuildEditorConfig(
            new OnlyOfficeEditorConfigRequest
            {
                DocumentId = doc.Id,
                VersionId = version.Id,
                VersionCreatedAt = version.CreatedAt,
                FileName = version.FileName,
                Edit = edit,
                Review = review,
                Download = true,
                Mode = mode,
                UserId = userId,
                UserName = userName
            }
        );

        return Result.Success(config);
    }
}
