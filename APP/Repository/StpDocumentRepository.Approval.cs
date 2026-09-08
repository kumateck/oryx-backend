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
    public async Task<Result<StpDocumentDto>> Approve(Guid documentId, Guid userId, string meaning, string password)
    {
        var passwordCheck = await VerifyPassword(userId, password);
        if (passwordCheck.IsFailure)
            return Result.Failure<StpDocumentDto>(passwordCheck.Error);

        if (string.IsNullOrWhiteSpace(meaning))
            return Result.Failure<StpDocumentDto>(StpDocumentErrors.MeaningRequired);

        var doc = await context.StpDocuments.FirstOrDefaultAsync(d => d.Id == documentId);
        if (doc == null)
            return Result.Failure<StpDocumentDto>(StpDocumentErrors.NotFound(documentId));

        if (!StpDocumentWorkflowPolicy.CanTransition(doc.Status, StpDocumentStatus.Approved))
            return Result.Failure<StpDocumentDto>(
                StpDocumentErrors.InvalidTransition(doc.Status, StpDocumentStatus.Approved)
            );

        if (doc.CurrentDraftVersionId == null)
            return Result.Failure<StpDocumentDto>(StpDocumentErrors.NoDraftVersion);

        var version = await context.StpDocumentVersions
            .Include(item => item.Signatures)
            .FirstAsync(item => item.Id == doc.CurrentDraftVersionId.Value);
        var fileAvailable = await EnsureVersionBlobAvailable(version.Id);
        if (fileAvailable.IsFailure)
            return Result.Failure<StpDocumentDto>(fileAvailable.Error);
        var reviewSignature = version.Signatures
            .Where(item => item.Action == StpDocumentSignatureAction.Reviewed)
            .OrderByDescending(item => item.CreatedAt)
            .FirstOrDefault();
        if (reviewSignature == null)
            return Result.Failure<StpDocumentDto>(StpDocumentErrors.ReviewRequired);
        if (!StpDocumentWorkflowPolicy.HasIndependentApprover(
                version.CreatedById,
                reviewSignature.CreatedById,
                userId))
            return Result.Failure<StpDocumentDto>(StpDocumentErrors.SegregationOfDuties);

        context.StpDocumentSignatures.Add(
            new StpDocumentSignature
            {
                StpDocumentVersionId = doc.CurrentDraftVersionId.Value,
                Action = StpDocumentSignatureAction.Approved,
                Meaning = meaning.Trim(),
                CreatedById = userId
            }
        );

        doc.EffectiveVersionId = doc.CurrentDraftVersionId;
        doc.Status = StpDocumentStatus.Approved;
        doc.LockedById = null;
        doc.LockedAt = null;
        doc.LastUpdatedById = userId;
        doc.UpdatedAt = DateTime.UtcNow;
        context.StpDocuments.Update(doc);
        await context.SaveChangesAsync();

        var reloaded = await LoadForDto(doc.Id);
        return Result.Success(ToDto(reloaded));
    }

    public async Task<Result<StpDocumentDto>> Reject(Guid documentId, Guid userId, string meaning, string password)
    {
        var passwordCheck = await VerifyPassword(userId, password);
        if (passwordCheck.IsFailure)
            return Result.Failure<StpDocumentDto>(passwordCheck.Error);

        if (string.IsNullOrWhiteSpace(meaning))
            return Result.Failure<StpDocumentDto>(StpDocumentErrors.MeaningRequired);

        var doc = await context.StpDocuments.FirstOrDefaultAsync(d => d.Id == documentId);
        if (doc == null)
            return Result.Failure<StpDocumentDto>(StpDocumentErrors.NotFound(documentId));

        if (!StpDocumentWorkflowPolicy.CanTransition(doc.Status, StpDocumentStatus.Draft))
            return Result.Failure<StpDocumentDto>(
                StpDocumentErrors.InvalidTransition(doc.Status, StpDocumentStatus.Draft)
            );

        if (doc.CurrentDraftVersionId == null)
            return Result.Failure<StpDocumentDto>(StpDocumentErrors.NoDraftVersion);

        context.StpDocumentSignatures.Add(
            new StpDocumentSignature
            {
                StpDocumentVersionId = doc.CurrentDraftVersionId.Value,
                Action = StpDocumentSignatureAction.Rejected,
                Meaning = meaning.Trim(),
                CreatedById = userId
            }
        );

        doc.Status = StpDocumentStatus.Draft;
        doc.LastUpdatedById = userId;
        doc.UpdatedAt = DateTime.UtcNow;
        context.StpDocuments.Update(doc);
        await context.SaveChangesAsync();

        var reloaded = await LoadForDto(doc.Id);
        return Result.Success(ToDto(reloaded));
    }

    private async Task<Result> VerifyPassword(Guid userId, string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return Result.Failure(UserErrors.IncorrectCredentials);

        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user?.PasswordHash == null)
            return Result.Failure(UserErrors.IncorrectCredentials);

        var verifyResult = userManager.PasswordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        return verifyResult == PasswordVerificationResult.Failed
            ? Result.Failure(UserErrors.IncorrectCredentials)
            : Result.Success();
    }
}
