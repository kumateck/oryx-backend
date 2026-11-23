using System.Data;
using APP.Services.Authorization;
using APP.Services.Reviews;
using APP.Utils;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Costing;
using DOMAIN.Entities.Reviews;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SHARED;

namespace APP.Repository;

internal static class CostingPriceListApprovalHandler
{
    internal const string ModelType = "CostingPriceList";

    internal static async Task CreateAsync(ApplicationDbContext context, Guid id, List<ApprovalStage> stages, Approval approval, Guid? actor)
    {
        if (await context.CostingPriceListApprovals.AnyAsync(stage => stage.CostingPriceListId == id)) return;
        if (!actor.HasValue || !(await CurrentPermissionSet.Load(context, actor.Value)).Contains(PermissionKeys.CanReviewCostingPriceList))
            throw new UnauthorizedAccessException("The price-list reviewer grant changed before approval stages could be created.");
        context.CostingPriceListApprovals.AddRange(stages.Select((stage, index) => new CostingPriceListApproval
        {
            Id = Guid.NewGuid(), CostingPriceListId = id, ApprovalId = approval.Id, UserId = stage.UserId, RoleId = stage.RoleId,
            Required = stage.Required, Order = stage.Order, CreatedAt = DateTime.UtcNow, ActivatedAt = index == 0 ? DateTime.UtcNow : null,
        }));
        await context.SaveChangesAsync();
    }

    internal static async Task<Result> Decide(ApplicationDbContext context, Guid id, Guid actor, string comments, bool approve)
    {
        try { return await DecideCore(context,id,actor,comments,approve); }
        catch (DbUpdateConcurrencyException) { return Stale; }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: "23505" or "40001" }) { return Stale; }
        catch (PostgresException exception) when (exception.SqlState is "23505" or "40001") { return Stale; }
    }
    private static Error Stale => Error.Conflict("CostingPriceList.Stale", "The revision changed. Reload it before deciding.");
    private static async Task<Result> DecideCore(ApplicationDbContext context, Guid id, Guid actor, string comments, bool approve)
    {
        await using var transaction = context.Database.IsRelational() ? await context.Database.BeginTransactionAsync(IsolationLevel.Serializable) : null;
        var key = approve ? PermissionKeys.CanApproveCostingPriceList : PermissionKeys.CanRejectCostingPriceList;
        if (!(await CurrentPermissionSet.Load(context, actor)).Contains(key)) return Error.Forbidden("CostingPriceList.Permission", $"You need {key}.");
        var list = await context.CostingPriceLists.Include(item => item.Approvals).FirstOrDefaultAsync(item => item.Id == id);
        if (list is null) return Error.NotFound("CostingPriceList.NotFound", "Price-list revision not found.");
        var review = await DocumentReviewGate.LatestAsync(context, ModelType, id);
        if (list.Status != CostingPriceListStatus.Pending || review?.Status != DocumentReviewStatus.Confirmed)
            return Error.Conflict("CostingPriceList.Review", "The revision must pass review before an approval decision.");
        var roles = await context.UserRoles.Where(role => role.UserId == actor).Select(role => role.RoleId).ToListAsync();
        var stage = list.Approvals.Where(item => item.Status == ApprovalStatus.Pending && item.ActivatedAt.HasValue)
            .OrderBy(item => item.Order).FirstOrDefault();
        if (stage is null || stage.UserId != actor && (!stage.RoleId.HasValue || !roles.Contains(stage.RoleId.Value)))
            return Error.Forbidden("CostingPriceList.Assignment", "You are not assigned to the active approval stage.");
        var next = approve ? list.Approvals.Where(item => item.Status == ApprovalStatus.Pending && item.Order > stage.Order)
            .OrderBy(item => item.Order).FirstOrDefault() : null;
        var final = approve && (next is null || list.Approvals.Where(item => item.Required && item.Id != stage.Id)
            .All(item => item.Status == ApprovalStatus.Approved));
        if (final)
        {
            var latest = await context.CostingPriceLists.AsNoTracking().Where(item => item.Division == list.Division && item.Status == CostingPriceListStatus.Approved)
                .OrderByDescending(item => item.RevisionNumber).FirstOrDefaultAsync();
            if (latest?.Id != list.PreviousRevisionId || latest is not null &&
                (latest.RevisionNumber >= list.RevisionNumber || latest.EffectiveFrom >= list.EffectiveFrom))
                return Error.Conflict("CostingPriceList.Previous", "The approved predecessor changed. Prepare a new revision from the latest list.");
        }
        var now = DateTime.UtcNow;
        stage.Status = approve ? ApprovalStatus.Approved : ApprovalStatus.Rejected;
        stage.ApprovedById = actor; stage.ApprovalTime = now; stage.Comments = comments?.Trim();
        if (final) { list.Status = CostingPriceListStatus.Approved; list.ApprovedAt = now; }
        else if (!approve) list.Status = CostingPriceListStatus.Rejected;
        else if (next is not null) next.ActivatedAt = now;
        list.Version++; list.UpdatedAt = now; list.LastUpdatedById = actor;
        context.ApprovalActionLogs.Add(new ApprovalActionLog { ModelId = id, UserId = actor, Status = stage.Status, Comments = comments?.Trim() });
        if (!(await CurrentPermissionSet.Load(context, actor)).Contains(key)) return Error.Forbidden("CostingPriceList.Permission", $"You need {key}.");
        await context.SaveChangesAsync();
        if (transaction is not null) await transaction.CommitAsync();
        return Result.Success();
    }
}
