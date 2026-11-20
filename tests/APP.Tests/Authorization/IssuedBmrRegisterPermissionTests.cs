using System.Reflection;
using API.Controllers;
using APP.Utils;
using Microsoft.AspNetCore.Authorization;
using Xunit;
namespace APP.Tests.Authorization;

public class IssuedBmrRegisterPermissionTests
{
    [Fact]
    public void Printing_the_issued_register_needs_its_own_key_and_is_never_anonymous()
    {
        var action = typeof(IssuedBmrController).GetMethod(nameof(IssuedBmrController.Register))!;
        Assert.Equal(PermissionKeys.CanPrintIssuedBmrRecords, Assert.Single(action.GetCustomAttributes<AuthorizeAttribute>()).Policy);
        Assert.Null(action.GetCustomAttribute<AllowAnonymousAttribute>());
        // The controller still requires viewing the records.
        Assert.Equal(PermissionKeys.CanViewIssuedBmrRecords, Assert.Single(typeof(IssuedBmrController).GetCustomAttributes<AuthorizeAttribute>()).Policy);
    }
}
