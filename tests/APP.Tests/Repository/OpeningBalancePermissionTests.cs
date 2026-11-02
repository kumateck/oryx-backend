using System.Reflection;
using API.Controllers;
using APP.Utils;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace APP.Tests.Repository;

public class OpeningBalancePermissionTests
{
    [Fact]
    public void Read_upload_correction_and_approval_are_separate_backend_policies()
    {
        Assert.Equal(PermissionKeys.CanViewOpeningBalances, Assert.Single(typeof(StockMigrationController).GetCustomAttributes<AuthorizeAttribute>()).Policy);
        var actions = new Dictionary<string, string>
        {
            [nameof(StockMigrationController.Upload)] = PermissionKeys.CanUploadOpeningBalances,
            [nameof(StockMigrationController.UpdateLine)] = PermissionKeys.CanCorrectOpeningBalances,
            [nameof(StockMigrationController.BulkUpdate)] = PermissionKeys.CanCorrectOpeningBalances,
            [nameof(StockMigrationController.UpdateUnits)] = PermissionKeys.CanCorrectOpeningBalances,
            [nameof(StockMigrationController.Recheck)] = PermissionKeys.CanCorrectOpeningBalances,
            [nameof(StockMigrationController.Discard)] = PermissionKeys.CanCorrectOpeningBalances,
            [nameof(StockMigrationController.Approve)] = PermissionKeys.CanApproveOpeningBalances,
        };
        foreach (var (name, permission) in actions)
        {
            var method = typeof(StockMigrationController).GetMethod(name) ?? throw new InvalidOperationException(name);
            Assert.Equal(permission, Assert.Single(method.GetCustomAttributes<AuthorizeAttribute>()).Policy);
        }
        var keys = actions.Values.Append(PermissionKeys.CanViewOpeningBalances).Distinct();
        foreach (var key in keys)
            Assert.Single(PermissionUtils.GeneratePermissions(), p => p.Key == key && p.Module == PermissionModules.Settings);
    }

    [Fact]
    public void Legacy_direct_upload_requires_opening_balance_access_and_refuses_stock_writes()
    {
        var method = typeof(MaterialController).GetMethod(nameof(MaterialController.UploadMaterialStock))
            ?? throw new InvalidOperationException("Legacy stock upload action is missing");
        Assert.Equal(new[] { PermissionKeys.CanUploadOpeningBalances, PermissionKeys.CanViewOpeningBalances },
            method.GetCustomAttributes<AuthorizeAttribute>().Select(a => a.Policy).OrderBy(p => p).ToArray());
        // The disabled action has no dependency on the repository or file payload.
        var result = new MaterialController(null!).UploadMaterialStock(null!);
        Assert.Equal(400, Assert.IsAssignableFrom<Microsoft.AspNetCore.Http.IStatusCodeHttpResult>(result).StatusCode);
    }
}
