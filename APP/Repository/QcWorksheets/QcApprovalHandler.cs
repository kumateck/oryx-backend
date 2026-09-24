using APP.Services.QcWorksheets;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.QcWorksheets;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository.QcWorksheets;

/// <summary>
/// Bridges the rebuilt QC module into the existing generic approval engine, following the
/// same shape as <c>RndProjectApprovalHandler</c> — stage rows created up front, one stage
/// approved per call, next order activated, owning entity flipped when all required stages
/// are approved.
/// <para>
/// Two QC-specific differences, both deliberate:
/// 1. Stage rows live in the single shared <see cref="QcApproval"/> table addressed by
///    (EntityType, EntityId), not in a per-entity table.
/// 2. Approving or rejecting requires a confirmed re-authentication
///    (<see cref="IQcReauthContext"/>); without it the call is refused, which is what stops
///    the generic approval endpoint from signing a QC document.
/// </para>
/// </summary>
internal static class QcApprovalHandler
{
    internal static async Task CreateAsync(
        ApplicationDbContext context,
        string modelType,
        Guid entityId,
        IReadOnlyCollection<ApprovalStage> stages,
        Approval approval)
    {
        var entityType = QcApprovalEntityTypes.FromModelType(modelType);
        if (entityType is null)
            throw new NotSupportedException($"'{modelType}' is not a QC worksheet model type.");

        var existing = await context.QcApprovals
            .Where(item => item.EntityType == entityType && item.EntityId == entityId)
            .ToListAsync();

        var round = 1;
        if (existing.Count > 0)
        {
            var latestRound = existing.Max(item => item.ApprovalRound);
            var latestRoundStages = existing.Where(item => item.ApprovalRound == latestRound).ToList();

            // If the latest round has not been acted on at all, a repeated submit is
            // idempotent rather than a new round.
            if (latestRoundStages.All(item => item.Status == ApprovalStatus.Pending)
                && latestRoundStages.All(item => item.ApprovalTime is null))
                return;

            // Otherwise the document was edited after being reviewed, so review starts
            // over in a new round and the previous round stays as history.
            round = latestRound + 1;
        }

        await context.QcApprovals.AddRangeAsync(stages.Select(stage => new QcApproval
        {
            Id = Guid.NewGuid(),
            EntityType = entityType,
            EntityId = entityId,
            ApprovalId = approval.Id,
            ApprovalRound = round,
            Required = stage.Required,
            Order = stage.Order,
            UserId = stage.UserId,
            RoleId = stage.RoleId,
            Status = ApprovalStatus.Pending,
            ActivatedAt = stage.Order == 1 ? DateTime.UtcNow : null,
            // Left null on purpose: nobody has re-authenticated yet. Stamped at approve time.
            ReauthConfirmedAt = null
        }));

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Records a single signed action that is not part of a multi-stage chain — currently
    /// manual supersession. Still requires a confirmed re-authentication, and still lands in
    /// the same shared table so every QC signature is queryable in one place.
    /// </summary>
    internal static async Task<Result> RecordSignedActionAsync(
        ApplicationDbContext context,
        IQcReauthContext reauth,
        string modelType,
        Guid entityId,
        Guid approvalId,
        Guid userId,
        string comments)
    {
        var entityType = QcApprovalEntityTypes.FromModelType(modelType);
        if (entityType is null)
            return Error.Validation(
                "QcApproval.UnsupportedModelType",
                $"'{modelType}' is not a QC worksheet model type.");

        if (reauth?.ConfirmedAt is null || reauth.ConfirmedUserId != userId)
            return QcWorksheetErrors.ReauthenticationRequired;

        var nextRound = await context.QcApprovals
            .Where(item => item.EntityType == entityType && item.EntityId == entityId)
            .Select(item => (int?)item.ApprovalRound)
            .MaxAsync() ?? 0;

        context.QcApprovals.Add(new QcApproval
        {
            Id = Guid.NewGuid(),
            EntityType = entityType,
            EntityId = entityId,
            ApprovalId = approvalId,
            ApprovalRound = nextRound + 1,
            Order = 1,
            Required = true,
            UserId = userId,
            Status = ApprovalStatus.Approved,
            ApprovalTime = DateTime.UtcNow,
            ApprovedById = userId,
            ActivatedAt = DateTime.UtcNow,
            Comments = comments,
            ReauthConfirmedAt = reauth.ConfirmedAt
        });

        await context.SaveChangesAsync();
        return Result.Success();
    }

    internal static async Task<Result> ApproveAsync(
        ApplicationDbContext context,
        IQcReauthContext reauth,
        string modelType,
        Guid entityId,
        Guid userId,
        IReadOnlyCollection<Guid> roleIds,
        string comments)
        => await TransitionAsync(
            context, reauth, modelType, entityId, userId, roleIds, comments, ApprovalStatus.Approved);

    internal static async Task<Result> RejectAsync(
        ApplicationDbContext context,
        IQcReauthContext reauth,
        string modelType,
        Guid entityId,
        Guid userId,
        IReadOnlyCollection<Guid> roleIds,
        string comments)
        => await TransitionAsync(
            context, reauth, modelType, entityId, userId, roleIds, comments, ApprovalStatus.Rejected);

    private static async Task<Result> TransitionAsync(
        ApplicationDbContext context,
        IQcReauthContext reauth,
        string modelType,
        Guid entityId,
        Guid userId,
        IReadOnlyCollection<Guid> roleIds,
        string comments,
        ApprovalStatus outcome)
    {
        var entityType = QcApprovalEntityTypes.FromModelType(modelType);
        if (entityType is null)
            return Error.Validation(
                "QcApproval.UnsupportedModelType",
                $"'{modelType}' is not a QC worksheet model type.");

        // Meaning-of-signature gate. A valid session is not enough for a QC signature.
        if (reauth?.ConfirmedAt is null || reauth.ConfirmedUserId != userId)
            return QcWorksheetErrors.ReauthenticationRequired;

        var stages = await context.QcApprovals
            .Where(item => item.EntityType == entityType && item.EntityId == entityId)
            .ToListAsync();

        if (stages.Count == 0)
            return Error.Validation(
                "QcApproval.NoStages",
                "This document has no approval stages. Submit it for review first.");

        var currentRound = stages.Max(item => item.ApprovalRound);
        var roundStages = stages.Where(item => item.ApprovalRound == currentRound).ToList();

        var stage = GetAssignedStage(roundStages, userId, roleIds);
        if (stage is null)
            return Error.Forbidden(
                "QcApproval.Unauthorized",
                "You are not authorized to approve this document at this time.");

        stage.Status = outcome;
        stage.ApprovalTime = DateTime.UtcNow;
        stage.ApprovedById = userId;
        stage.Comments = comments;
        stage.ReauthConfirmedAt = reauth.ConfirmedAt;

        if (outcome == ApprovalStatus.Approved)
        {
            ActivateNextOrder(roundStages);

            var allRequiredApproved = roundStages
                .Where(item => item.Required)
                .All(item => item.Status == ApprovalStatus.Approved);

            if (allRequiredApproved)
                await MarkEntityApprovedAsync(context, entityType, entityId);
        }
        else
        {
            // A rejection sends the document back to Draft: a reviewer must not be left
            // evaluating a record that still claims to be under review.
            await MarkEntityRejectedAsync(context, entityType, entityId);
        }

        await context.SaveChangesAsync();
        return Result.Success();
    }

    private static QcApproval GetAssignedStage(
        IEnumerable<QcApproval> stages,
        Guid userId,
        IReadOnlyCollection<Guid> roleIds)
    {
        return stages
            .Where(item => item.Status == ApprovalStatus.Pending)
            .OrderByDescending(item => item.Required)
            .ThenBy(item => item.Order)
            .FirstOrDefault(item => item.UserId == userId
                || (item.RoleId.HasValue && roleIds.Contains(item.RoleId.Value)));
    }

    private static void ActivateNextOrder(IEnumerable<QcApproval> stages)
    {
        var pending = stages.Where(item => item.Status == ApprovalStatus.Pending).ToList();
        if (pending.Count == 0)
            return;

        var nextOrder = pending.Min(item => item.Order);
        foreach (var stage in pending.Where(item => item.Order == nextOrder && item.ActivatedAt is null))
            stage.ActivatedAt = DateTime.UtcNow;
    }

    private static async Task MarkEntityApprovedAsync(
        ApplicationDbContext context,
        string entityType,
        Guid entityId)
    {
        switch (entityType)
        {
            case QcApprovalEntityTypes.StandardTestProcedure:
                var stp = await context.QcStandardTestProcedures
                    .SingleOrDefaultAsync(item => item.Id == entityId);
                if (stp is null) return;
                stp.Approved = true;
                stp.Status = QcDocumentStatus.Approved;
                stp.UpdatedAt = DateTime.UtcNow;
                break;

            case QcApprovalEntityTypes.WorksheetTemplate:
                var template = await context.QcWorksheetTemplates
                    .SingleOrDefaultAsync(item => item.Id == entityId);
                if (template is null) return;
                template.Approved = true;
                template.Status = QcDocumentStatus.Approved;
                template.UpdatedAt = DateTime.UtcNow;
                break;

            case QcApprovalEntityTypes.Specification:
                var specification = await context.QcSpecifications
                    .SingleOrDefaultAsync(item => item.Id == entityId);
                if (specification is null) return;
                specification.Approved = true;
                specification.Status = QcDocumentStatus.Approved;
                specification.UpdatedAt = DateTime.UtcNow;
                break;

            // A worksheet carries the execution lifecycle rather than the controlled-document
            // one, so an approved review lands on Reviewed, not "Approved".
            case QcApprovalEntityTypes.WorksheetInstance:
                var instance = await context.QcWorksheetInstances
                    .SingleOrDefaultAsync(item => item.Id == entityId);
                if (instance is null) return;
                instance.Approved = true;
                instance.Status = WorksheetInstanceStatus.Reviewed;
                instance.UpdatedAt = DateTime.UtcNow;
                break;
        }
    }

    private static async Task MarkEntityRejectedAsync(
        ApplicationDbContext context,
        string entityType,
        Guid entityId)
    {
        switch (entityType)
        {
            case QcApprovalEntityTypes.StandardTestProcedure:
                var stp = await context.QcStandardTestProcedures
                    .SingleOrDefaultAsync(item => item.Id == entityId);
                if (stp is null) return;
                stp.Approved = false;
                stp.Status = QcDocumentStatus.Draft;
                stp.UpdatedAt = DateTime.UtcNow;
                break;

            case QcApprovalEntityTypes.WorksheetTemplate:
                var template = await context.QcWorksheetTemplates
                    .SingleOrDefaultAsync(item => item.Id == entityId);
                if (template is null) return;
                template.Approved = false;
                template.Status = QcDocumentStatus.Draft;
                template.UpdatedAt = DateTime.UtcNow;
                break;

            case QcApprovalEntityTypes.Specification:
                var specification = await context.QcSpecifications
                    .SingleOrDefaultAsync(item => item.Id == entityId);
                if (specification is null) return;
                specification.Approved = false;
                specification.Status = QcDocumentStatus.Draft;
                specification.UpdatedAt = DateTime.UtcNow;
                break;

            // A rejected review is a return for correction: the worksheet goes back to the
            // analyst, in progress, with its entered values and its assignee intact.
            case QcApprovalEntityTypes.WorksheetInstance:
                var instance = await context.QcWorksheetInstances
                    .SingleOrDefaultAsync(item => item.Id == entityId);
                if (instance is null) return;
                instance.Approved = false;
                instance.Status = WorksheetInstanceStatus.InProgress;
                instance.SubmittedAt = null;
                instance.UpdatedAt = DateTime.UtcNow;
                break;
        }
    }
}
