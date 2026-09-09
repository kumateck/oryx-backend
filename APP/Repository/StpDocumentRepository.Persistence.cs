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
    public async Task<Result<(Stream Stream, string ContentType, string Name)>> GetVersionFile(
        Guid documentId,
        Guid versionId
    )
    {
        var version = await context.StpDocumentVersions.FirstOrDefaultAsync(v =>
            v.Id == versionId && v.StpDocumentId == documentId
        );
        if (version == null)
            return Result.Failure<(Stream, string, string)>(StpDocumentErrors.VersionNotFound(versionId));

        var blobResult = await blobStorageService.GetBlobAsync(BucketName, version.StorageKey);
        if (blobResult.IsFailure)
            return Result.Failure<(Stream, string, string)>(blobResult.Error);

        var (stream, _, _) = blobResult.Value;
        return Result.Success<(Stream, string, string)>((stream, WordMimeType, version.FileName));
    }

    private async Task<Result> EnsureVersionBlobAvailable(Guid versionId)
    {
        var storageKey = await context.StpDocumentVersions
            .Where(version => version.Id == versionId)
            .Select(version => version.StorageKey)
            .FirstOrDefaultAsync();
        if (storageKey == null)
            return Result.Failure(StpDocumentErrors.VersionNotFound(versionId));

        var exists = await blobStorageService.BlobExistsAsync(BucketName, storageKey);
        if (exists.IsFailure)
            return Result.Failure(exists.Error);
        return exists.Value
            ? Result.Success()
            : Result.Failure(StpDocumentErrors.StoredFileUnavailable);
    }

    private async Task<Result> ValidateOwner(string ownerType, Guid ownerId)
    {
        bool exists;
        if (ownerType == nameof(MaterialStandardTestProcedure))
            exists = await context.MaterialStandardTestProcedures.AnyAsync(item => item.Id == ownerId);
        else if (ownerType == nameof(ProductStandardTestProcedure))
            exists = await context.ProductStandardTestProcedures.AnyAsync(item => item.Id == ownerId);
        else
            return Result.Failure(StpDocumentErrors.InvalidOwnerType);

        return exists
            ? Result.Success()
            : Result.Failure(StpDocumentErrors.OwnerNotFound(ownerId));
    }

    private async Task<StpDocument> LoadForDto(string ownerType, Guid ownerId)
    {
        return await context
            .StpDocuments.Include(d => d.LockedBy)
            .Include(d => d.Versions)
            .ThenInclude(v => v.CreatedBy)
            .Include(d => d.Versions)
            .ThenInclude(v => v.Signatures)
            .ThenInclude(s => s.CreatedBy)
            .FirstOrDefaultAsync(d => d.OwnerType == ownerType && d.OwnerId == ownerId);
    }

    private async Task<StpDocument> LoadForDto(Guid documentId)
    {
        return await context
            .StpDocuments.Include(d => d.LockedBy)
            .Include(d => d.Versions)
            .ThenInclude(v => v.CreatedBy)
            .Include(d => d.Versions)
            .ThenInclude(v => v.Signatures)
            .ThenInclude(s => s.CreatedBy)
            .FirstOrDefaultAsync(d => d.Id == documentId);
    }

    private StpDocumentDto ToDto(StpDocument doc)
    {
        var dto = mapper.Map<StpDocumentDto>(doc);

        var effectiveVersionNumber = doc.EffectiveVersionId.HasValue
            ? doc.Versions.FirstOrDefault(v => v.Id == doc.EffectiveVersionId)?.VersionNumber
            : null;

        dto.Versions = doc
            .Versions.OrderByDescending(v => v.VersionNumber)
            .Select(v =>
            {
                var versionDto = mapper.Map<StpDocumentVersionDto>(v);
                versionDto.IsEffective = doc.EffectiveVersionId.HasValue && v.Id == doc.EffectiveVersionId;
                versionDto.IsSuperseded = effectiveVersionNumber.HasValue && v.VersionNumber < effectiveVersionNumber.Value;
                return versionDto;
            })
            .ToList();
        return dto;
    }
}
