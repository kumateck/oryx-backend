using System.Data;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Customers;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

internal static class CustomerQuotationApprovalHandler
{
    internal static async Task CreateAsync(
        ApplicationDbContext context,
        Guid quotationId,
        List<ApprovalStage> stages,
        Approval approval)
    {
        if (await context.CustomerQuotationApprovals.AnyAsync(item =>
                item.CustomerQuotationId == quotationId && item.ApprovalId == approval.Id))
            return;

        var approvals = stages.Select((stage, index) => new CustomerQuotationApproval
        {
            CustomerQuotationId = quotationId,
            ApprovalId = approval.Id,
            Order = stage.Order,
            Required = stage.Required,
            UserId = stage.UserId,
            RoleId = stage.RoleId,
            ActivatedAt = index == 0 ? DateTime.UtcNow : null,
            CreatedAt = DateTime.UtcNow,
        });
        await context.CustomerQuotationApprovals.AddRangeAsync(approvals);
        await context.SaveChangesAsync();
    }

    internal static Task<Result> ApproveAsync(
        ApplicationDbContext context,
        Guid quotationId,
        Guid userId,
        List<Guid> roleIds,
        string comments)
        => ReviewAsync(context, quotationId, userId, roleIds, comments, ApprovalStatus.Approved);

    internal static Task<Result> RejectAsync(
        ApplicationDbContext context,
        Guid quotationId,
        Guid userId,
        List<Guid> roleIds,
        string comments)
        => ReviewAsync(context, quotationId, userId, roleIds, comments, ApprovalStatus.Rejected);

    private static async Task<Result> ReviewAsync(
        ApplicationDbContext context,
        Guid quotationId,
        Guid userId,
        List<Guid> roleIds,
        string comments,
        ApprovalStatus decision)
    {
        await using var transaction = context.Database.IsRelational()
            ? await context.Database.BeginTransactionAsync(IsolationLevel.Serializable)
            : null;
        var quotation = await context.CustomerQuotations
            .Include(item => item.Approvals)
            .FirstOrDefaultAsync(item => item.Id == quotationId);
        if (quotation is null)
            return Error.NotFound("CustomerQuotation.NotFound", "Quotation not found.");
        if (quotation.Status != CustomerQuotationStatus.Sent)
            return Error.Conflict("CustomerQuotation.Status", "Only a sent quotation can be reviewed.");
        if (quotation.ValidUntil < DateTime.UtcNow)
            return Error.Validation("CustomerQuotation.Expired", "An expired quotation cannot be reviewed.");
        if (quotation.CreatedById == userId)
            return Error.Validation(
                "CustomerQuotation.MakerChecker",
                "The quotation creator cannot approve or reject it.");

        var stage = quotation.Approvals
            .Where(item => item.Status == ApprovalStatus.Pending && item.ActivatedAt.HasValue)
            .OrderBy(item => item.Order)
            .FirstOrDefault();
        if (stage is null)
            return Error.Conflict(
                "CustomerQuotation.NoActiveStage",
                "No quotation approval stage is active.");
        var assigned = stage.UserId == userId
            || stage.RoleId.HasValue && roleIds?.Contains(stage.RoleId.Value) == true;
        if (!assigned)
            return Error.Validation(
                "Approval.Unauthorized",
                "You are not authorized to review this quotation at this time.");

        stage.Status = decision;
        stage.Comments = comments?.Trim();
        stage.ApprovalTime = DateTime.UtcNow;
        stage.ApprovedById = userId;
        context.ApprovalActionLogs.Add(new ApprovalActionLog
        {
            ModelId = quotation.Id,
            UserId = userId,
            Status = decision,
            Comments = comments?.Trim(),
        });

        if (decision == ApprovalStatus.Rejected)
        {
            quotation.Status = CustomerQuotationStatus.Rejected;
            quotation.Approved = false;
        }
        else
        {
            var next = quotation.Approvals
                .Where(item => item.Status == ApprovalStatus.Pending && item.Order > stage.Order)
                .OrderBy(item => item.Order)
                .FirstOrDefault();
            if (next is null)
            {
                quotation.Status = CustomerQuotationStatus.Accepted;
                quotation.Approved = true;
            }
            else
            {
                next.ActivatedAt = DateTime.UtcNow;
            }
        }

        quotation.UpdatedAt = DateTime.UtcNow;
        quotation.LastUpdatedById = userId;
        await context.SaveChangesAsync();
        if (transaction is not null) await transaction.CommitAsync();
        return Result.Success();
    }
}
