using System.Reflection;
using API.Controllers;
using APP.Utils;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace APP.Tests.QcWorksheets;

/// <summary>Pins the intentional split between the self-scoped queue and the global audit view.</summary>
public class QcWorksheetApprovalPermissionTests
{
    [Fact]
    public void Only_the_non_self_scoped_approval_history_requires_the_history_key()
    {
        var pendingPolicies = typeof(QcWorksheetApprovalController)
            .GetMethod(nameof(QcWorksheetApprovalController.GetMyPending))!
            .GetCustomAttributes<AuthorizeAttribute>()
            .Select(attribute => attribute.Policy)
            .ToList();
        var historyPolicy = Assert.Single(
            typeof(QcWorksheetApprovalController)
                .GetMethod(nameof(QcWorksheetApprovalController.GetForEntity))!
                .GetCustomAttributes<AuthorizeAttribute>()
                .Select(attribute => attribute.Policy));

        Assert.Empty(pendingPolicies);
        Assert.Equal(QcWorksheetPermissionKeys.CanViewQcApprovalHistory, historyPolicy);
    }

    [Fact]
    public void Approval_history_key_is_grantable_under_its_own_submodule()
    {
        var permission = Assert.Single(
            PermissionUtils.GeneratePermissions(),
            item => item.Key == QcWorksheetPermissionKeys.CanViewQcApprovalHistory);

        Assert.Equal(QcWorksheetPermissionCatalog.Module, permission.Module);
        Assert.Equal(QcWorksheetPermissionCatalog.ApprovalHistory, permission.SubModule);
    }
}
