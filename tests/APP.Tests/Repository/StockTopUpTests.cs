using API.Controllers;
using APP.Repository;
using APP.Utils;
using DOMAIN.Entities.Materials.StockMigrations;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace APP.Tests.Repository;

public class StockTopUpTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Reason_is_required(string? reason) => Assert.NotNull(StockMigrationRepository.ReasonError(reason!));

    [Fact]
    public void Reason_has_a_bounded_length()
    {
        Assert.NotNull(StockMigrationRepository.ReasonError(new string('x', 2001)));
        Assert.Null(StockMigrationRepository.ReasonError("Missed during migration"));
    }

    [Fact]
    public void Different_receipt_references_keep_separate_top_up_lots()
    {
        var draft = new StockMigration { Mode = StockMigrationMode.TopUp };
        var first = StockMigrationRepository.BatchIdentity(draft, Guid.NewGuid(), " B-01 ", "GRN-1", "AR-1");
        Assert.Equal(first, StockMigrationRepository.BatchIdentity(draft, Guid.NewGuid(), "b-01", "GRN-1", "AR-1"));
        Assert.NotEqual(first, StockMigrationRepository.BatchIdentity(draft, Guid.NewGuid(), "B-01", "GRN-2", "AR-1"));
        Assert.NotEqual(first, StockMigrationRepository.BatchIdentity(draft, Guid.NewGuid(), "B-01", "GRN-1", "AR-2"));
        draft.Mode = StockMigrationMode.OpeningBalance;
        Assert.Equal(StockMigrationRepository.BatchIdentity(draft, Guid.NewGuid(), "B-01", "GRN-1", "AR-1"),
            StockMigrationRepository.BatchIdentity(draft, Guid.NewGuid(), "B-01", "GRN-2", "AR-2"));
    }

    [Fact]
    public void Lot_dates_compare_month_and_year_only()
    {
        Assert.True(StockMigrationRepository.SameMonth(new DateTime(2028, 2, 1), new DateTime(2028, 2, 29)));
        Assert.False(StockMigrationRepository.SameMonth(new DateTime(2028, 2, 29), new DateTime(2028, 3, 1)));
        Assert.False(StockMigrationRepository.SameMonth(new DateTime(2028, 2, 1), null));
        Assert.True(StockMigrationRepository.SameMonth(null, null));
        var draft = new StockMigration { Mode = StockMigrationMode.TopUp };
        Assert.NotEqual(StockMigrationRepository.BatchIdentity(draft, Guid.NewGuid(), "", "", ""),
            StockMigrationRepository.BatchIdentity(draft, Guid.NewGuid(), "", "", ""));
    }

    [Fact]
    public void Top_up_endpoints_keep_the_existing_separate_permissions()
    {
        Assert.Contains(typeof(StockMigrationController).GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>(),
            a => a.Policy == PermissionKeys.CanViewOpeningBalances);
        foreach (var method in new[] { "CreateTopUp", "UploadTopUp", "Approve" })
        {
            var info = typeof(StockMigrationController).GetMethod(method);
            Assert.NotNull(info);
            var expected = method == "Approve" ? PermissionKeys.CanApproveOpeningBalances : PermissionKeys.CanUploadOpeningBalances;
            Assert.Contains(info.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>(), a => a.Policy == expected);
        }
    }
}
