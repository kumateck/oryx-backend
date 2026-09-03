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
        var response = await context.Responses.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == responseId);
        if (response is null)
            return Error.NotFound("Response.NotFound", "Response not found.");

        var configurations = await context.Approvals.AsNoTracking()
            .Include(item => item.ApprovalStages)
            .Where(item => item.ItemType == nameof(Response)).Take(2).ToListAsync();
        if (configurations.Count > 1)
            return Error.Conflict("Response.ApprovalAmbiguous",
                "Multiple response approval configurations exist.");

        var stages = configurations.SingleOrDefault()?.ApprovalStages ?? [];
        if (stages.Any(item => !item.UserId.HasValue && !item.RoleId.HasValue))
            return Error.Validation("Response.ApprovalAssignment",
                "Every response approval stage must be assigned to a user or role.");

        var existing = await context.ResponseApprovals.AsNoTracking()
            .Where(item => item.ResponseId == responseId).ToListAsync();
        var current = ResponseApprovalRoundManager.Current(existing);
        if (current.Any(item => item.Status == ApprovalStatus.Pending))
            return Error.Conflict("Response.ApprovalPending",
                "This response already has a pending approval round.");
        if (response.Approved || current.Any(item => item.Status == ApprovalStatus.Approved))
            return Error.Conflict("Response.AlreadyApproved",
                "This response has already been approved. Start an audited revision instead.");
        if (response.Rejected || current.Any(item => item.Status == ApprovalStatus.Rejected))
            return Error.Conflict("Response.RevisionRequired",
                "This response was rejected. Start an audited revision before resubmitting.");
        if (existing.Count > 0)
            return Error.Conflict("Response.RevisionRequired",
                "This response has a completed approval round. Start an audited revision before resubmitting.");
        return Result.Success();
    }
}
