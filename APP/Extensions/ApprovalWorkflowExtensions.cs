using DOMAIN.Entities.Approvals;
using SHARED;

namespace APP.Extensions;

public static class ApprovalWorkflowExtensions
{
    public static Result EnsureApprovedForProgression(
        this IRequireApproval document,
        string documentName,
        string errorCode = "Approval.Required"
    ) =>
        document.Approved
            ? Result.Success()
            : Error.Validation(
                errorCode,
                $"{documentName} is awaiting approval and cannot proceed."
            );
}
