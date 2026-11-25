using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace INFRASTRUCTURE.Migrations;

/// <summary>
/// Receiving transferred stock uses the existing transfer assignment keys, independently of supplier issue.
/// Preserve the access types of roles that could receive under the former issue permission check.
/// </summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20261125120000_GrantStockTransferReceiptAssignments")]
public sealed class GrantStockTransferReceiptAssignments : Migration
{
    public static readonly (string Key, string From)[] Grants =
    [
        ("CanAssignRawMaterialStockTransfers", "CanIssueRawMaterialStockTransfers"),
        ("CanAssignPackagingMaterialStockTransfers", "CanIssuePackagingMaterialStockTransfers"),
    ];

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        foreach (var (key, from) in Grants)
            migrationBuilder.Sql(GrantProductSectionPermissions.Grant(key, from));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Both keys predate this migration. Preserve existing and administrator-assigned grants.
    }
}
