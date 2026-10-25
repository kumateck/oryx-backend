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

    [Theory]
    [InlineData(typeof(MaterialRetestController), nameof(MaterialRetestController.List),
        QcWorksheetPermissionKeys.CanViewMaterialRetests, QcWorksheetPermissionCatalog.Retests)]
    [InlineData(typeof(MaterialRetestController), nameof(MaterialRetestController.Get),
        QcWorksheetPermissionKeys.CanViewMaterialRetestDetail, QcWorksheetPermissionCatalog.Retests)]
    [InlineData(typeof(MaterialRetestController), nameof(MaterialRetestController.Approve),
        QcWorksheetPermissionKeys.CanApproveMaterialRetest, QcWorksheetPermissionCatalog.Retests)]
    [InlineData(typeof(MaterialRetestController), nameof(MaterialRetestController.Reject),
        QcWorksheetPermissionKeys.CanRejectMaterialRetest, QcWorksheetPermissionCatalog.Retests)]
    [InlineData(typeof(QcResponseReworkController), nameof(QcResponseReworkController.List),
        QcWorksheetPermissionKeys.CanViewResponseReworkTasks, QcWorksheetPermissionCatalog.Rework)]
    [InlineData(typeof(QcResponseReworkController), nameof(QcResponseReworkController.Get),
        QcWorksheetPermissionKeys.CanViewResponseReworkTaskDetail, QcWorksheetPermissionCatalog.Rework)]
    [InlineData(typeof(QcResponseReworkController), nameof(QcResponseReworkController.Start),
        QcWorksheetPermissionKeys.CanStartResponseReworkTask, QcWorksheetPermissionCatalog.Rework)]
    public void Retest_and_rework_endpoints_have_grantable_policies(
        Type controller, string action, string key, string subModule)
    {
        var policy = Assert.Single(controller.GetMethod(action)!
            .GetCustomAttributes<AuthorizeAttribute>()
            .Select(attribute => attribute.Policy));
        Assert.Equal(key, policy);
        var permission = Assert.Single(PermissionUtils.GeneratePermissions(), item => item.Key == key);
        Assert.Equal(QcWorksheetPermissionCatalog.Module, permission.Module);
        Assert.Equal(subModule, permission.SubModule);
    }

    [Theory]
    [InlineData(PermissionKeys.CanApproveResponse)]
    [InlineData(PermissionKeys.CanRejectResponse)]
    public void Response_decision_permissions_are_individually_grantable(string key)
    {
        var permission = Assert.Single(PermissionUtils.GeneratePermissions(), item => item.Key == key);
        Assert.Equal(PermissionModules.QualityAssurance, permission.Module);
        Assert.Equal(PermissionSubmodules.PendingApprovals, permission.SubModule);
    }
}
