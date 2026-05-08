using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseOrderToItemInvoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PurchaseOrderId",
                table: "ItemShipmentInvoiceItems",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_ItemShipmentInvoiceItems_PurchaseOrderId",
                table: "ItemShipmentInvoiceItems",
                column: "PurchaseOrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_ItemShipmentInvoiceItems_PurchaseOrders_PurchaseOrderId",
                table: "ItemShipmentInvoiceItems",
                column: "PurchaseOrderId",
                principalTable: "PurchaseOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ItemShipmentInvoiceItems_PurchaseOrders_PurchaseOrderId",
                table: "ItemShipmentInvoiceItems");

            migrationBuilder.DropIndex(
                name: "IX_ItemShipmentInvoiceItems_PurchaseOrderId",
                table: "ItemShipmentInvoiceItems");

            migrationBuilder.DropColumn(
                name: "PurchaseOrderId",
                table: "ItemShipmentInvoiceItems");
        }
    }
}
