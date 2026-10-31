using System.Reflection;
using API.Controllers;
using APP.Utils;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace APP.Tests.Users;

public class SwitchDepartmentPermissionTests
{
    [Fact]
    public void Switching_department_requires_the_permission_and_no_role()
    {
        var authorize = Assert.Single(typeof(UserController).GetMethod(nameof(UserController.SwitchDepartment))!
            .GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal(PermissionKeys.CanSwitchDepartment, authorize.Policy);
        Assert.Null(authorize.Roles);
    }

    [Fact]
    public void Switch_department_permission_is_grantable_under_user_management()
    {
        var permission = Assert.Single(PermissionUtils.GeneratePermissions(), item => item.Key == PermissionKeys.CanSwitchDepartment);
        Assert.Equal(PermissionModules.ItSupport, permission.Module);
        Assert.Equal(PermissionSubmodules.UserManagement, permission.SubModule);
        Assert.Equal(["Access"], permission.Types);
    }
}
