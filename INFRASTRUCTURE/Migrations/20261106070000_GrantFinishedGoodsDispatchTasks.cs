using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <summary>
    /// Production order writes now check their keys on the server. Marking an allocation loaded or delivered was gated on
    /// the desktop by the packing-list or invoice view key; each now has its own key, granted with the same access types
    /// to every role holding either view key, so the people who could act still can. Editing or deleting an order,
    /// deleting a proforma invoice, customer invoices and sending a waybill to the customer get keys granted to nobody:
    /// no page offers them. Data only; the model is unchanged.
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261106070000_GrantFinishedGoodsDispatchTasks")]
    public partial class GrantFinishedGoodsDispatchTasks : Migration
    {
        public static readonly (string Key, string From)[] Grants =
        [
            ("CanLoadFinishedGoods", "CanViewPackingList"),
            ("CanLoadFinishedGoods", "CanViewInvoice"),
            ("CanDeliverFinishedGoods", "CanViewPackingList"),
            ("CanDeliverFinishedGoods", "CanViewInvoice"),
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
            foreach (var key in Grants.Select(grant => grant.Key).Distinct())
                migrationBuilder.Sql($$"""
                    DELETE FROM "PermissionTypes" WHERE "Key" = '{{key}}';
                    DELETE FROM "roleclaims" WHERE "ClaimType" = 'permission' AND "ClaimValue" = '{{key}}';
                    """);
        }
    }
}
