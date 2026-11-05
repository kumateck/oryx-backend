using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <summary>
    /// Production's "view requisitions" tick boxes were aliases of the Warehouse issue-list keys, so granting one granted
    /// both (permissions audit 3.3). Production now has its own keys; every role holding the shared keys gets them with the
    /// same access types, so nobody loses access, and administrators can now grant the two departments separately.
    /// Data only; the model is unchanged.
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261105210000_GrantProductionRequisitionViews")]
    public partial class GrantProductionRequisitionViews : Migration
    {
        public static readonly (string Key, string From)[] Grants =
        [
            ("CanViewProductionRawMaterialRequisitions", "CanViewRawMaterialRequisitions"),
            ("CanViewProductionPackagingMaterialRequisitions", "CanViewPackagingMaterialRequisitions"),
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
