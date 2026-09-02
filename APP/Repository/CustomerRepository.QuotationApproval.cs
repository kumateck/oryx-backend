using System.Data;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Customers;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public partial class CustomerRepository
{
    public async Task<Result> SendQuotation(Guid quotationId, Guid userId)
    {
        await using var transaction = context.Database.IsRelational()
            ? await context.Database.BeginTransactionAsync(IsolationLevel.Serializable) : null;
        var quotation = await context.CustomerQuotations.Include(item => item.Approvals)
            .FirstOrDefaultAsync(item => item.Id == quotationId);
        if (quotation is null) return Error.NotFound("CustomerQuotation.NotFound", "Quotation not found.");
        if (quotation.Status != CustomerQuotationStatus.Draft)
            return Error.Conflict("CustomerQuotation.Status", "Only a draft quotation can be sent.");
        if (quotation.ValidUntil < DateTime.UtcNow)
            return Error.Validation("CustomerQuotation.Expired", "An expired quotation cannot be sent.");

        var configurations = await context.Approvals.AsNoTracking().Include(item => item.ApprovalStages)
            .Where(item => item.ItemType == nameof(CustomerQuotation)).Take(2).ToListAsync();
        switch (configurations.Count)
        {
            case 0:
                return Error.Validation("CustomerQuotation.ApprovalMissing", "Configure quotation approval stages before sending.");
            case > 1:
                return Error.Conflict("CustomerQuotation.ApprovalAmbiguous", "Multiple quotation approval configurations exist.");
        }

        var configuration = configurations[0];
        var stages = configuration.ApprovalStages.OrderBy(item => item.Order).ToList();
        if (stages.Count == 0)
            return Error.Validation("CustomerQuotation.ApprovalMissing", "Quotation approval has no stages.");

        quotation.Approvals =
        [
            .. stages.Select((stage, index) => new CustomerQuotationApproval
            {
                CustomerQuotationId = quotation.Id, ApprovalId = configuration.Id,
                Order = stage.Order, Required = stage.Required, UserId = stage.UserId, RoleId = stage.RoleId,
                ActivatedAt = index == 0 ? DateTime.UtcNow : null, CreatedAt = DateTime.UtcNow,
            })
        ];
        quotation.Status = CustomerQuotationStatus.Sent;
        await context.SaveChangesAsync();
        if (transaction is not null) await transaction.CommitAsync();
        return Result.Success();
    }

    public async Task<Result> ApproveQuotation(
        Guid quotationId, CustomerQuotationApprovalRequest request, Guid userId)
    {
        await using var transaction = context.Database.IsRelational()
            ? await context.Database.BeginTransactionAsync(IsolationLevel.Serializable) : null;
        if (request.Status is not ApprovalStatus.Approved and not ApprovalStatus.Rejected)
            return Error.Validation("CustomerQuotation.ApprovalStatus", "Approval must be approved or rejected.");
        var quotation = await context.CustomerQuotations.Include(item => item.Approvals)
            .FirstOrDefaultAsync(item => item.Id == quotationId);
        if (quotation is null) return Error.NotFound("CustomerQuotation.NotFound", "Quotation not found.");
        if (quotation.Status != CustomerQuotationStatus.Sent)
            return Error.Conflict("CustomerQuotation.Status", "Only a sent quotation can be approved.");
        if (quotation.ValidUntil < DateTime.UtcNow)
            return Error.Validation("CustomerQuotation.Expired", "An expired quotation cannot be approved.");
        if (quotation.CreatedById == userId)
            return Error.Validation("CustomerQuotation.MakerChecker", "The quotation creator cannot approve it.");

        var stage = quotation.Approvals.Where(item => item.Status == ApprovalStatus.Pending
                && item.ActivatedAt.HasValue).OrderBy(item => item.Order).FirstOrDefault();
        if (stage is null) return Error.Conflict("CustomerQuotation.NoActiveStage", "No approval stage is active.");
        if (!await UserCanApprove(stage, userId))
            return Error.Validation("CustomerQuotation.Approver", "The active stage is not assigned to this user.");

        stage.Status = request.Status;
        stage.Comments = request.Comments?.Trim();
        stage.ApprovalTime = DateTime.UtcNow;
        stage.ApprovedById = userId;
        context.ApprovalActionLogs.Add(new ApprovalActionLog
        {
            ModelId = quotation.Id, UserId = userId, Status = request.Status,
            Comments = request.Comments?.Trim(),
        });
        if (request.Status == ApprovalStatus.Rejected)
        {
            quotation.Status = CustomerQuotationStatus.Rejected;
            quotation.Approved = false;
        }
        else
        {
            var next = quotation.Approvals.Where(item => item.Status == ApprovalStatus.Pending
                    && item.Order > stage.Order).OrderBy(item => item.Order).FirstOrDefault();
            if (next is null)
            {
                quotation.Status = CustomerQuotationStatus.Accepted;
                quotation.Approved = true;
            }
            else next.ActivatedAt = DateTime.UtcNow;
        }
        quotation.UpdatedAt = DateTime.UtcNow;
        quotation.LastUpdatedById = userId;
        await context.SaveChangesAsync();
        if (transaction is not null) await transaction.CommitAsync();
        return Result.Success();
    }

    private async Task<bool> UserCanApprove(CustomerQuotationApproval stage, Guid userId)
    {
        if (stage.UserId == userId) return true;
        return stage.RoleId.HasValue && await context.UserRoles
            .AnyAsync(item => item.UserId == userId && item.RoleId == stage.RoleId.Value);
    }
}
