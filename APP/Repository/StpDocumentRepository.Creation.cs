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
    public async Task<Result<StpDocumentDto>> GetOrNull(string ownerType, Guid ownerId)
    {
        var owner = await ValidateOwner(ownerType, ownerId);
        if (owner.IsFailure)
            return Result.Failure<StpDocumentDto>(owner.Error);
        var doc = await LoadForDto(ownerType, ownerId);
        return doc == null ? Result.Success<StpDocumentDto>(null) : Result.Success(ToDto(doc));
    }

    public async Task<Result<StpDocumentDto>> CreateBlank(string ownerType, Guid ownerId, Guid userId)
    {
        var owner = await ValidateOwner(ownerType, ownerId);
        if (owner.IsFailure)
            return Result.Failure<StpDocumentDto>(owner.Error);

        var existing = await context.StpDocuments.FirstOrDefaultAsync(d =>
            d.OwnerType == ownerType && d.OwnerId == ownerId
        );
        if (existing != null)
            return Result.Failure<StpDocumentDto>(StpDocumentErrors.AlreadyExists);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var doc = new StpDocument
            {
                OwnerType = ownerType,
                OwnerId = ownerId,
                Status = StpDocumentStatus.Draft,
                CreatedById = userId
            };
            context.StpDocuments.Add(doc);
            await context.SaveChangesAsync();

            const string fileName = "New Document.docx";
            var bytes = GenerateBlankDocx();
            var storageKey = $"{doc.Id}/v1.docx";

            var uploadFile = WrapAsFormFile(bytes, fileName, WordMimeType);
            var uploadResult = await blobStorageService.UploadBlobAsync(BucketName, uploadFile, storageKey);
            if (uploadResult.IsFailure)
            {
                await transaction.RollbackAsync();
                return Result.Failure<StpDocumentDto>(uploadResult.Error);
            }

            var version = new StpDocumentVersion
            {
                StpDocumentId = doc.Id,
                VersionNumber = 1,
                StorageKey = storageKey,
                FileName = fileName,
                Sha256 = ComputeSha256(bytes),
                Size = bytes.LongLength,
                Source = StpDocumentVersionSource.Created,
                CreatedById = userId
            };
            context.StpDocumentVersions.Add(version);
            await context.SaveChangesAsync();

            doc.CurrentDraftVersionId = version.Id;
            context.StpDocuments.Update(doc);
            await context.SaveChangesAsync();

            await transaction.CommitAsync();

            var reloaded = await LoadForDto(doc.Id);
            return Result.Success(ToDto(reloaded));
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
