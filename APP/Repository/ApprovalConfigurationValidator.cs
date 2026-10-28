using DOMAIN.Entities.Approvals;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

internal static class ApprovalConfigurationValidator
{
    internal static async Task<Result> ValidateAssignees(ApplicationDbContext context, CreateApprovalRequest request)
    {
        var stages = request.ApprovalStages ?? [];
        var roles = stages.Where(stage => stage.RoleId.HasValue).Select(stage => stage.RoleId!.Value).Distinct().ToList();
        var users = stages.Where(stage => stage.UserId.HasValue).Select(stage => stage.UserId!.Value).Distinct().ToList();
        if (await context.Roles.CountAsync(role => roles.Contains(role.Id) && role.DeletedAt == null) != roles.Count
            || await context.Users.CountAsync(user => users.Contains(user.Id) && user.DeletedAt == null && !user.IsDisabled) != users.Count)
            return Error.Validation("Approval.ApproverUnavailable", "Choose an existing role or active user for every approval stage.");
        return Result.Success();
    }

    internal static Result Validate(CreateApprovalRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ItemType))
            return Error.Validation("Approval.ItemType", "Approval item type is required.");

        var stages = request.ApprovalStages ?? [];
        if (stages.Any(stage => stage.UserId.HasValue == stage.RoleId.HasValue))
            return Error.Validation(
                "Approval.StageAssignment",
                "Every approval stage must be assigned to exactly one user or role.");

        if (stages.Any(stage => stage.Order < 1)
            || stages.Select(stage => stage.Order).Distinct().Count() != stages.Count)
            return Error.Validation(
                "Approval.StageOrder",
                "Approval stage orders must be positive and unique.");

        return Result.Success();
    }
}
