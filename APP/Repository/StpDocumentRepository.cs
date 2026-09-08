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

public partial class StpDocumentRepository : IStpDocumentRepository
{
    private const string BucketName = "stp-documents";
    private const string WordMimeType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
    private const long MaxFileSizeBytes = 25 * 1024 * 1024; // 25MB
    private static readonly TimeSpan LockTtl = TimeSpan.FromMinutes(30);

    private readonly ApplicationDbContext context;
    private readonly IBlobStorageService blobStorageService;
    private readonly IOnlyOfficeConfigService onlyOfficeConfigService;
    private readonly IHttpClientFactory httpClientFactory;
    private readonly UserManager<User> userManager;
    private readonly IMapper mapper;
    private readonly ILogger<StpDocumentRepository> logger;

    public StpDocumentRepository(
        ApplicationDbContext context,
        IBlobStorageService blobStorageService,
        IOnlyOfficeConfigService onlyOfficeConfigService,
        IHttpClientFactory httpClientFactory,
        UserManager<User> userManager,
        IMapper mapper,
        ILogger<StpDocumentRepository> logger)
    {
        this.context = context;
        this.blobStorageService = blobStorageService;
        this.onlyOfficeConfigService = onlyOfficeConfigService;
        this.httpClientFactory = httpClientFactory;
        this.userManager = userManager;
        this.mapper = mapper;
        this.logger = logger;
    }

    public async Task<Result<StpDocumentDto>> UploadVersion(
        string ownerType,
        Guid ownerId,
        IFormFile file,
        Guid userId,
        string reasonForChange
    )
    {
        var owner = await ValidateOwner(ownerType, ownerId);
        if (owner.IsFailure)
            return Result.Failure<StpDocumentDto>(owner.Error);

        if (file != null)
        {
            var validation = ValidateDocxFile(file);
            if (validation.IsFailure) return Result.Failure<StpDocumentDto>(validation.Error);
        }

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var doc = await context
                .StpDocuments.Include(d => d.Versions)
                .FirstOrDefaultAsync(d => d.OwnerType == ownerType && d.OwnerId == ownerId);

            if (doc == null)
            {
                if (file == null)
                {
                    await transaction.RollbackAsync();
                    return Result.Failure<StpDocumentDto>(StpDocumentErrors.FileRequired);
                }

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
            {
                await transaction.RollbackAsync();
                return Result.Failure<StpDocumentDto>(
                    StpDocumentErrors.VersionChangeNotAllowed(doc.Status)
                );
            }
            if (startingNewDraftFromApproved && string.IsNullOrWhiteSpace(reasonForChange))
            {
                await transaction.RollbackAsync();
                return Result.Failure<StpDocumentDto>(StpDocumentErrors.ReasonForChangeRequired);
            }

            if (file == null && !startingNewDraftFromApproved)
            {
                await transaction.RollbackAsync();
                return Result.Failure<StpDocumentDto>(StpDocumentErrors.FileRequired);
            }

            var nextVersionNumber = (doc.Versions.Count != 0 ? doc.Versions.Max(v => v.VersionNumber) : 0) + 1;
            var storageKey = $"{doc.Id}/v{nextVersionNumber}.docx";

            string fileName;
            string sha256;
            long size;

            if (file != null)
            {
                await using var stream = file.OpenReadStream();
                using var buffer = new MemoryStream();
                await stream.CopyToAsync(buffer);
                var bytes = buffer.ToArray();

                sha256 = ComputeSha256(bytes);
                size = bytes.LongLength;
                fileName = Path.GetFileName(file.FileName);

                var uploadFile = WrapAsFormFile(bytes, fileName, file.ContentType ?? WordMimeType);
                var uploadResult = await blobStorageService.UploadBlobAsync(BucketName, uploadFile, storageKey);
                if (uploadResult.IsFailure)
                {
                    await transaction.RollbackAsync();
                    return Result.Failure<StpDocumentDto>(uploadResult.Error);
                }
            }
            else
            {
                // Start a new draft from the approved doc without a fresh upload: clone the
                // effective version's blob content under the new version's own storage key.
                if (doc.EffectiveVersionId == null)
                {
                    await transaction.RollbackAsync();
                    return Result.Failure<StpDocumentDto>(StpDocumentErrors.NoEffectiveVersion);
                }

                var effectiveVersion = doc.Versions.FirstOrDefault(v => v.Id == doc.EffectiveVersionId)
                    ?? await context.StpDocumentVersions.FirstOrDefaultAsync(v => v.Id == doc.EffectiveVersionId);

                var blobResult = await blobStorageService.GetBlobAsync(BucketName, effectiveVersion.StorageKey);
                if (blobResult.IsFailure)
                {
                    await transaction.RollbackAsync();
                    return Result.Failure<StpDocumentDto>(blobResult.Error);
                }

                var (sourceStream, _, _) = blobResult.Value;
                using var buffer = new MemoryStream();
                await sourceStream.CopyToAsync(buffer);
                var bytes = buffer.ToArray();

                sha256 = ComputeSha256(bytes);
                size = bytes.LongLength;
                fileName = effectiveVersion.FileName;

                var uploadFile = WrapAsFormFile(bytes, fileName, WordMimeType);
                var uploadResult = await blobStorageService.UploadBlobAsync(BucketName, uploadFile, storageKey);
                if (uploadResult.IsFailure)
                {
                    await transaction.RollbackAsync();
                    return Result.Failure<StpDocumentDto>(uploadResult.Error);
                }
            }

            var version = new StpDocumentVersion
            {
                StpDocumentId = doc.Id,
                VersionNumber = nextVersionNumber,
                StorageKey = storageKey,
                FileName = fileName,
                Sha256 = sha256,
                Size = size,
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
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
