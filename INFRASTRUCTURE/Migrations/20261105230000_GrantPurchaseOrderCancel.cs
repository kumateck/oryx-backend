using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <summary>
    /// Cancelling a purchase order had no permission (any signed-in user could), and the desktop asked for the revise key
    /// instead. Cancel now has its own foreign and local keys, granted with the same access types to every role holding the
    /// matching revise key, so the people who could cancel still can. Deleting an order gets its own keys too, granted to
    /// nobody until assigned. Data only; the model is unchanged.
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261105230000_GrantPurchaseOrderCancel")]
    public partial class GrantPurchaseOrderCancel : Migration
    {
        public static readonly (string Key, string From)[] Grants =
        [
            ("CanCancelForeignPurchaseOrder", "CanReviseForeignPurchaseOrder"),
            ("CanCancelLocalPurchaseOrder", "CanReviseLocalPurchaseOrder"),
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var (key, from) in Grants)
                migrationBuilder.Sql(GrantProductSectionPermissions.Grant(key, from));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var (key, _) in Grants)
                migrationBuilder.Sql($$"""
                    DELETE FROM "PermissionTypes" WHERE "Key" = '{{key}}';
                    DELETE FROM "roleclaims" WHERE "ClaimType" = 'permission' AND "ClaimValue" = '{{key}}';
                    """);
        }
    }
}
