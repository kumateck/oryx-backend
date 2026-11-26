using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace INFRASTRUCTURE.Migrations;

/// <summary>Whoever could change a linked material's unit through the stock-levels dialog keeps that task.</summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20261126100000_GrantMaterialCountingUnitChange")]
public sealed class GrantMaterialCountingUnitChange : Migration
{
    public static readonly (string Key, string From)[] Grants =
    [
        ("CanChangeRawMaterialCountingUnit", "CanEditLinkedRawMaterials"),
        ("CanChangePackagingMaterialCountingUnit", "CanLinkPackagingMaterials"),
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
