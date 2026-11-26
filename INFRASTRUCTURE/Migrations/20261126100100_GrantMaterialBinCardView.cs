using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace INFRASTRUCTURE.Migrations;

/// <summary>Preserves bin-card access for the approved-material and expired-material screens that serve this route.</summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20261126100100_GrantMaterialBinCardView")]
public sealed class GrantMaterialBinCardView : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        foreach (var from in new[] { "CanViewApprovedRawMaterials", "CanViewApprovedPackagingMaterials", "CanViewExpiredMaterials" })
            migrationBuilder.Sql(GrantProductSectionPermissions.Grant("CanViewMaterialBinCard", from));
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        DELETE FROM "PermissionTypes" WHERE "Key" = 'CanViewMaterialBinCard';
        DELETE FROM "roleclaims" WHERE "ClaimType" = 'permission' AND "ClaimValue" = 'CanViewMaterialBinCard';
        """);
}
