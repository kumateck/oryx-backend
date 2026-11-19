using APP.Utils;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace APP.Tests.Repository;

/// <summary>Printing a product COA gets its own key; every role that checks product tests keeps the task on deploy.</summary>
public sealed class PrintCertificateOfAnalysisGrantTests
{
    [RepairPostgresFact]
    public async Task Deployment_grants_the_print_key_once_to_roles_that_check_product_tests()
    {
        await using var fixture = new IssuedMaterialRepairFixture();
        await fixture.Seed(DOMAIN.Entities.Products.Production.IssuedMaterialRepairKind.Bom, issueStock: false);
        await using var db = fixture.Open();
        await SharedDensityTransferTests.Grant(db, fixture.Actor.UserId!.Value, PermissionKeys.CanCheckProductTest);
        await db.Database.ExecuteSqlRawAsync("SELECT setval(pg_get_serial_sequence('roleclaims', 'Id'), (SELECT MAX(\"Id\") FROM \"roleclaims\"))");
        Assert.DoesNotContain(PermissionKeys.CanPrintProductCertificateOfAnalysis, await APP.Services.Authorization.CurrentPermissionSet.Load(db, fixture.Actor.UserId));
        var migration = new INFRASTRUCTURE.Migrations.GrantPrintProductCertificateOfAnalysis();
        var generator = Microsoft.EntityFrameworkCore.Infrastructure.AccessorExtensions.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrationsSqlGenerator>(db);
        for (var pass = 0; pass < 2; pass++)
            foreach (var command in generator.Generate(migration.UpOperations, db.Model))
                await db.Database.ExecuteSqlRawAsync(command.CommandText);
        db.ChangeTracker.Clear();
        Assert.Contains(PermissionKeys.CanPrintProductCertificateOfAnalysis, await APP.Services.Authorization.CurrentPermissionSet.Load(db, fixture.Actor.UserId));
        var claim = await db.RoleClaims.SingleAsync(c => c.ClaimValue == PermissionKeys.CanPrintProductCertificateOfAnalysis);
        Assert.Equal("Action", (await db.PermissionTypes.SingleAsync(p => p.RoleClaimId == claim.Id)).Type);
    }
}
