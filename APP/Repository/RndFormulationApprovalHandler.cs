using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.RndFormulations;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

internal static class RndFormulationApprovalHandler
{
    internal static async Task CreateAsync(ApplicationDbContext context, Guid id,
        IReadOnlyCollection<ApprovalStage> stages, Approval approval)
    {
        var existing = await context.RndFormulationApprovals
            .Where(item => item.RndFormulationId == id && item.ApprovalId == approval.Id)
            .ToListAsync();
        if (existing.Count != 0)
        {
            foreach (var item in existing)
            {
                item.Status = ApprovalStatus.Pending;
                item.ApprovalTime = null;
                item.ApprovedById = null;
                item.Comments = null;
                item.ActivatedAt = item.Order == stages.Min(stage => stage.Order) ? DateTime.UtcNow : null;
            }
        }
        else
            await context.RndFormulationApprovals.AddRangeAsync(stages.Select(stage =>
                new RndFormulationApprovals
                {
                    RndFormulationId = id, ApprovalId = approval.Id, Required = stage.Required,
                    Order = stage.Order, UserId = stage.UserId, RoleId = stage.RoleId,
                    ActivatedAt = stage.Order == stages.Min(item => item.Order) ? DateTime.UtcNow : null,
                }));
        await context.SaveChangesAsync();
    }

    internal static Task<Result> ApproveAsync(ApplicationDbContext context, Guid id, Guid userId,
        IReadOnlyCollection<Guid> roleIds, string comments) =>
        ReviewAsync(context, id, userId, roleIds, comments, ApprovalStatus.Approved);

    internal static Task<Result> RejectAsync(ApplicationDbContext context, Guid id, Guid userId,
        IReadOnlyCollection<Guid> roleIds, string comments) =>
        ReviewAsync(context, id, userId, roleIds, comments, ApprovalStatus.Rejected);

    private static async Task<Result> ReviewAsync(ApplicationDbContext context, Guid id, Guid userId,
        IReadOnlyCollection<Guid> roleIds, string comments, ApprovalStatus decision)
    {
        var formulation = await context.RndFormulations.Include(item => item.Approvals)
            .SingleOrDefaultAsync(item => item.Id == id);
        if (formulation is null) return Error.NotFound("RndFormulation.NotFound", "Formulation not found.");
        if (formulation.Status != RndFormulationStatus.InReview)
            return Error.Conflict("RndFormulation.Status", "Only a formulation in review can be decided.");
        if (formulation.CreatedById == userId)
            return Error.Validation("RndFormulation.MakerChecker", "The formulation creator cannot review it.");
        var stage = formulation.Approvals.Where(item => item.Status == ApprovalStatus.Pending && item.ActivatedAt.HasValue)
            .OrderBy(item => item.Order).FirstOrDefault(item => item.UserId == userId
                || item.RoleId.HasValue && roleIds.Contains(item.RoleId.Value));
        if (stage is null) return Error.Validation("Approval.Unauthorized", "No assigned formulation stage is active.");
        stage.Status = decision; stage.ApprovalTime = DateTime.UtcNow; stage.ApprovedById = userId;
        stage.Comments = comments?.Trim();
        context.ApprovalActionLogs.Add(new ApprovalActionLog
            { ModelId = id, UserId = userId, Status = decision, Comments = comments?.Trim() });
        if (decision == ApprovalStatus.Rejected)
        {
            formulation.Status = RndFormulationStatus.Draft; formulation.Approved = false;
        }
        else
        {
            var next = formulation.Approvals.Where(item => item.Status == ApprovalStatus.Pending && item.Order > stage.Order)
                .OrderBy(item => item.Order).FirstOrDefault();
            if (next is null) { formulation.Status = RndFormulationStatus.Approved; formulation.Approved = true; }
            else next.ActivatedAt = DateTime.UtcNow;
        }
        formulation.LastUpdatedById = userId;
        await context.SaveChangesAsync();
        return Result.Success();
    }
}
