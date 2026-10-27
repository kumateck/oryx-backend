using API.Controllers;
using APP.Utils;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace APP.Tests.Notifications;
public class AlertAuthorizationTests
{
    [Theory]
    [InlineData(nameof(AlertController.CreateAlert), PermissionKeys.CanCreateNewAlerts)]
    [InlineData(nameof(AlertController.GetAlert), PermissionKeys.CanViewAlerts)]
    [InlineData(nameof(AlertController.GetAlerts), PermissionKeys.CanViewAlerts)]
    [InlineData(nameof(AlertController.UpdateAlert), PermissionKeys.CanEditAlerts)]
    [InlineData(nameof(AlertController.ToggleDisable), PermissionKeys.CanEnableDisableAlerts)]
    [InlineData(nameof(AlertController.DeleteAlert), PermissionKeys.CanDeleteAlerts)]
    public void Configuration_requires_existing_backend_permissions(string method, string policy)
    {
        var action = typeof(AlertController).GetMethod(method)!;
        Assert.Contains(action.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>(), item => item.Policy == policy);
    }
    [Theory]
    [InlineData(nameof(AlertController.GetNotificationsForUser))]
    [InlineData(nameof(AlertController.MarkNotificationAsRead))]
    public void Personal_notifications_remain_authenticated_without_configuration_permissions(string method)
    {
        var action = typeof(AlertController).GetMethod(method)!;
        Assert.Contains(action.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>(), item => string.IsNullOrEmpty(item.Policy));
    }
}
