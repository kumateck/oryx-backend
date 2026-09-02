using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Forms;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

internal static class ResponseApprovalSubmission
{
    internal static async Task<Result> ValidateAsync(
        ApplicationDbContext context,
        Guid responseId)
    {
        var configurations = await context.Approvals.AsNoTracking()
            .Include(item => item.ApprovalStages)
            .Where(item => item.ItemType == nameof(Response)).Take(2).ToListAsync();
        switch (configurations.Count)
        {
            case 0:
                return Error.Validation("Response.Approval",
                    "Approval configuration for response does not exist.");
            case > 1:
                return Error.Conflict("Response.ApprovalAmbiguous",
                    "Multiple response approval configurations exist.");
        }

        var stages = configurations[0].ApprovalStages;
        if (stages.Count == 0)
            return Error.Validation("Response.ApprovalStages",
                "Response approval must have at least one stage.");
        if (stages.Any(item => !item.UserId.HasValue && !item.RoleId.HasValue))
            return Error.Validation("Response.ApprovalAssignment",
                "Every response approval stage must be assigned to a user or role.");

        var existing = await context.ResponseApprovals.AsNoTracking()
            .Where(item => item.ResponseId == responseId).ToListAsync();
        if (ResponseApprovalRoundManager.Current(existing)
            .Any(item => item.Status == ApprovalStatus.Pending))
            return Error.Conflict("Response.ApprovalPending",
                "This response already has a pending approval round.");
        return Result.Success();
    }
}
