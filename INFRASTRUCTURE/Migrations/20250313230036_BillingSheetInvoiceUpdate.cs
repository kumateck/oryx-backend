using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class BillingSheetInvoiceUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BillingSheets_PurchaseOrderInvoices_InvoiceId",
                table: "BillingSheets");

            migrationBuilder.AddForeignKey(
                name: "FK_BillingSheets_ShipmentInvoices_InvoiceId",
                table: "BillingSheets",
                column: "InvoiceId",
                principalTable: "ShipmentInvoices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BillingSheets_ShipmentInvoices_InvoiceId",
                table: "BillingSheets");

            migrationBuilder.AddForeignKey(
                name: "FK_BillingSheets_PurchaseOrderInvoices_InvoiceId",
                table: "BillingSheets",
                column: "InvoiceId",
                principalTable: "PurchaseOrderInvoices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
