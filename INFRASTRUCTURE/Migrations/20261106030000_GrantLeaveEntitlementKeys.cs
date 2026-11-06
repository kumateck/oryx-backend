using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <summary>
    /// HR and settings writes now check their keys on the server. Leave entitlements had no keys at all; they are leave
    /// configuration, so each entitlement task is granted, with the same access types, to every role holding the matching
    /// leave-type key. Deleting an employee or a leave request gets its own key, granted to nobody until assigned: no page
    /// offers either. Data only; the model is unchanged.
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261106030000_GrantLeaveEntitlementKeys")]
    public partial class GrantLeaveEntitlementKeys : Migration
    {
        public static readonly (string Key, string From)[] Grants =
        [
            ("CanCreateLeaveEntitlement", "CanCreateLeaveType"),
            ("CanEditLeaveEntitlement", "CanEditLeaveType"),
            ("CanDeleteLeaveEntitlement", "CanDeleteLeaveType"),
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
