using APP.Services.Storage;
using DOMAIN.Entities.StpDocuments;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public partial class StpDocumentRepository
{
    public async Task<Result<StpDocumentDto>> UploadVersion(
        string ownerType,
        Guid ownerId,
        IFormFile file,
        Guid userId,
        string reasonForChange)
    {
        var owner = await ValidateOwner(ownerType, ownerId);
        if (owner.IsFailure) return Result.Failure<StpDocumentDto>(owner.Error);

        byte[] uploadedBytes = null;
        string uploadedSha256 = null;
        if (file != null)
        {
            var validation = ValidateDocxFile(file);
            if (validation.IsFailure) return Result.Failure<StpDocumentDto>(validation.Error);

            await using var stream = file.OpenReadStream();
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer);
            uploadedBytes = buffer.ToArray();
            uploadedSha256 = ComputeSha256(uploadedBytes);
        }

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var doc = await context.StpDocuments
                .Include(d => d.Versions)
                .FirstOrDefaultAsync(d => d.OwnerType == ownerType && d.OwnerId == ownerId);

            if (doc == null)
            {
                if (file == null) return await Fail(StpDocumentErrors.FileRequired);
                doc = new StpDocument
                {
                    OwnerType = ownerType,
                    OwnerId = ownerId,
                    Status = StpDocumentStatus.Draft,
                    CreatedById = userId
                };
                context.StpDocuments.Add(doc);
                await context.SaveChangesAsync();
            }

            var startingNewDraftFromApproved = doc.Status == StpDocumentStatus.Approved;
            if (doc.Status is StpDocumentStatus.InReview or StpDocumentStatus.Reviewed)
                return await Fail(StpDocumentErrors.VersionChangeNotAllowed(doc.Status));
            if (startingNewDraftFromApproved && string.IsNullOrWhiteSpace(reasonForChange))
                return await Fail(StpDocumentErrors.ReasonForChangeRequired);
            if (file == null && !startingNewDraftFromApproved)
                return await Fail(StpDocumentErrors.FileRequired);

            if (uploadedSha256 != null && IsDuplicateDraftUpload(doc, uploadedSha256))
            {
                await transaction.RollbackAsync();
                var existing = await LoadForDto(doc.Id);
                return Result.Success(ToDto(existing));
            }

            var nextVersionNumber = (doc.Versions.Count != 0
                ? doc.Versions.Max(v => v.VersionNumber)
                : 0) + 1;
            var storageKey = $"{doc.Id}/v{nextVersionNumber}.docx";
            Result<(byte[] Bytes, string FileName, string ContentType)> content;
            if (uploadedBytes != null)
            {
                var uploadContentType = string.IsNullOrWhiteSpace(file!.ContentType)
                    ? WordMimeType
                    : file.ContentType;
                content = Result.Success((uploadedBytes, Path.GetFileName(file.FileName), uploadContentType));
            }
            else
            {
                content = await ReadEffectiveVersion(doc);
            }
            if (content.IsFailure) return await Fail(content.Error);

            var (bytes, fileName, contentType) = content.Value;
            var uploadFile = WrapAsFormFile(bytes, fileName, contentType);
            var uploadResult = await blobStorageService.UploadBlobAsync(BucketName, uploadFile, storageKey);
            if (uploadResult.IsFailure) return await Fail(uploadResult.Error);

            var version = new StpDocumentVersion
            {
                StpDocumentId = doc.Id,
                VersionNumber = nextVersionNumber,
                StorageKey = storageKey,
                FileName = fileName,
                Sha256 = ComputeSha256(bytes),
                Size = bytes.LongLength,
                Source = StpDocumentVersionSource.Upload,
                ReasonForChange = reasonForChange,
                CreatedById = userId
            };
            context.StpDocumentVersions.Add(version);
            await context.SaveChangesAsync();

            doc.CurrentDraftVersionId = version.Id;
            doc.Status = StpDocumentStatus.Draft;
            doc.LastUpdatedById = userId;
            doc.UpdatedAt = DateTime.UtcNow;
            context.StpDocuments.Update(doc);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            var reloaded = await LoadForDto(doc.Id);
            return Result.Success(ToDto(reloaded));
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        async Task<Result<StpDocumentDto>> Fail(Error error)
        {
            await transaction.RollbackAsync();
            return Result.Failure<StpDocumentDto>(error);
        }
    }

    internal static bool IsDuplicateDraftUpload(StpDocument doc, string sha256)
    {
        if (doc.Status != StpDocumentStatus.Draft || doc.CurrentDraftVersionId == null)
            return false;

        var currentDraft = doc.Versions.FirstOrDefault(v => v.Id == doc.CurrentDraftVersionId);
        return string.Equals(currentDraft?.Sha256, sha256, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<Result<(byte[] Bytes, string FileName, string ContentType)>> ReadEffectiveVersion(
        StpDocument doc)
    {
        if (doc.EffectiveVersionId == null)
            return Result.Failure<(byte[], string, string)>(StpDocumentErrors.NoEffectiveVersion);

        var version = doc.Versions.FirstOrDefault(v => v.Id == doc.EffectiveVersionId)
            ?? await context.StpDocumentVersions.FirstAsync(v => v.Id == doc.EffectiveVersionId);
        var blob = await blobStorageService.GetBlobAsync(BucketName, version.StorageKey);
        if (blob.IsFailure)
            return Result.Failure<(byte[], string, string)>(blob.Error);

        var (sourceStream, _, _) = blob.Value;
        await using (sourceStream)
        using (var buffer = new MemoryStream())
        {
            await sourceStream.CopyToAsync(buffer);
            return Result.Success((buffer.ToArray(), version.FileName, WordMimeType));
        }
    }
}
