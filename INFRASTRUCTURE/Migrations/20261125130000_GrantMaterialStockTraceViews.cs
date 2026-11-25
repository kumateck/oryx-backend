using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace INFRASTRUCTURE.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261125130000_GrantMaterialStockTraceViews")]
public sealed class GrantMaterialStockTraceViews : Migration
{
    public static readonly (string Key, string From)[] Grants =
    [
        ("CanViewRawMaterialStockTrace", "CanViewApprovedRawMaterials"),
        ("CanViewPackagingMaterialStockTrace", "CanViewApprovedPackagingMaterials"),
        ("CanPrintRawMaterialStockTrace", "CanViewApprovedRawMaterials"),
        ("CanPrintPackagingMaterialStockTrace", "CanViewApprovedPackagingMaterials"),
    ];
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        foreach (var (key, from) in Grants) migrationBuilder.Sql(GrantProductSectionPermissions.Grant(key, from));
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        foreach (var (key, _) in Grants)
            migrationBuilder.Sql($$"""
                DELETE FROM "PermissionTypes" WHERE "Key" = '{{key}}';
                DELETE FROM "roleclaims" WHERE "ClaimType" = 'permission' AND "ClaimValue" = '{{key}}';
                """);
    }
}
