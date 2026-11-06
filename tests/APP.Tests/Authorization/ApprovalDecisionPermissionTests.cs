using API.Controllers;
using APP.Utils;
using Xunit;

namespace APP.Tests.Authorization;

/// <summary>Approval decisions need a permission for their document type as well as being the configured approver.</summary>
public sealed class ApprovalDecisionPermissionTests
{
    [Theory]
    [InlineData("PurchaseOrder", true, PermissionKeys.CanApprovePendingApproval)]
    [InlineData("LeaveRequest", false, PermissionKeys.CanRejectPendingApproval)]
    [InlineData("PayrollRun", true, PermissionKeys.CanApprovePayrollRun)]
    [InlineData("PayrollRun", false, PermissionKeys.CanCancelPayrollRun)]
    public void Each_document_type_names_its_decision_key(string modelType, bool approve, string key) =>
        Assert.Equal(key, ApprovalController.DecisionKey(modelType, approve));

    [Theory]
    [InlineData("ShipmentDiscrepancy")]
    [InlineData("StockDiscrepancy")]
    [InlineData("Response")]
    public void Documents_with_their_own_checks_are_not_checked_twice(string modelType)
    {
        Assert.Null(ApprovalController.DecisionKey(modelType, true));
        Assert.Null(ApprovalController.DecisionKey(modelType, false));
    }
}
