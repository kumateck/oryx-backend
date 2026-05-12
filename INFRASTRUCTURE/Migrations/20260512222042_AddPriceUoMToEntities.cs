using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddPriceUoMToEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PriceUoM",
                table: "VendorQuotationItems",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PriceUoM",
                table: "RevisedPurchaseOrderItem",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PriceUoM",
                table: "RevisedPurchaseOrder",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PriceUoMBefore",
                table: "RevisedPurchaseOrder",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PriceUoM",
                table: "PurchaseOrderItems",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PriceUoM",
                table: "ProductPrices",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PriceUoM",
                table: "VendorQuotationItems");

            migrationBuilder.DropColumn(
                name: "PriceUoM",
                table: "RevisedPurchaseOrderItem");

            migrationBuilder.DropColumn(
                name: "PriceUoM",
                table: "RevisedPurchaseOrder");

            migrationBuilder.DropColumn(
                name: "PriceUoMBefore",
                table: "RevisedPurchaseOrder");

            migrationBuilder.DropColumn(
                name: "PriceUoM",
                table: "PurchaseOrderItems");

            migrationBuilder.DropColumn(
                name: "PriceUoM",
                table: "ProductPrices");
        }
    }
}
