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
    public async Task<Result<StpDocumentDto>> SubmitForReview(Guid documentId, Guid userId)
    {
        var doc = await context.StpDocuments.FirstOrDefaultAsync(d => d.Id == documentId);
        if (doc == null)
            return Result.Failure<StpDocumentDto>(StpDocumentErrors.NotFound(documentId));

        if (doc.Status != StpDocumentStatus.Draft)
            return Result.Failure<StpDocumentDto>(
                StpDocumentErrors.InvalidTransition(doc.Status, StpDocumentStatus.InReview)
            );

        if (doc.CurrentDraftVersionId == null)
            return Result.Failure<StpDocumentDto>(StpDocumentErrors.NoDraftVersion);

        var lockIsActive = doc.LockedById.HasValue
            && doc.LockedAt.HasValue
            && DateTime.UtcNow - doc.LockedAt.Value <= LockTtl;
        if (lockIsActive)
            return Result.Failure<StpDocumentDto>(StpDocumentErrors.EditorSessionActive);

        doc.Status = StpDocumentStatus.InReview;
        doc.LockedById = null;
        doc.LockedAt = null;
        doc.LastUpdatedById = userId;
        doc.UpdatedAt = DateTime.UtcNow;
        context.StpDocuments.Update(doc);
        await context.SaveChangesAsync();

        var reloaded = await LoadForDto(doc.Id);
        return Result.Success(ToDto(reloaded));
    }

    public async Task<Result<StpDocumentDto>> Review(Guid documentId, Guid userId, string meaning, string password)
    {
        var passwordCheck = await VerifyPassword(userId, password);
        if (passwordCheck.IsFailure)
            return Result.Failure<StpDocumentDto>(passwordCheck.Error);
        if (string.IsNullOrWhiteSpace(meaning))
            return Result.Failure<StpDocumentDto>(StpDocumentErrors.MeaningRequired);

        var doc = await context.StpDocuments.FirstOrDefaultAsync(item => item.Id == documentId);
        if (doc == null)
            return Result.Failure<StpDocumentDto>(StpDocumentErrors.NotFound(documentId));
        if (!StpDocumentWorkflowPolicy.CanTransition(doc.Status, StpDocumentStatus.Reviewed))
            return Result.Failure<StpDocumentDto>(
                StpDocumentErrors.InvalidTransition(doc.Status, StpDocumentStatus.Reviewed)
            );
        if (doc.CurrentDraftVersionId == null)
            return Result.Failure<StpDocumentDto>(StpDocumentErrors.NoDraftVersion);

        var version = await context.StpDocumentVersions.FirstAsync(item => item.Id == doc.CurrentDraftVersionId);
        if (!StpDocumentWorkflowPolicy.HasIndependentReviewer(version.CreatedById, userId))
            return Result.Failure<StpDocumentDto>(StpDocumentErrors.SegregationOfDuties);

        context.StpDocumentSignatures.Add(new StpDocumentSignature
        {
            StpDocumentVersionId = version.Id,
            Action = StpDocumentSignatureAction.Reviewed,
            Meaning = meaning.Trim(),
            CreatedById = userId
        });
        doc.Status = StpDocumentStatus.Reviewed;
        doc.LastUpdatedById = userId;
        doc.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        return Result.Success(ToDto(await LoadForDto(doc.Id)));
    }
}
