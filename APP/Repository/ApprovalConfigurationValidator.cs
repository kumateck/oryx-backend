using DOMAIN.Entities.Approvals;
using SHARED;

namespace APP.Repository;

internal static class ApprovalConfigurationValidator
{
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
