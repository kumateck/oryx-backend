using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class UpdateShipmentInvoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PurchaseOrderIds",
                table: "ShipmentInvoices");

            migrationBuilder.AddColumn<Guid>(
                name: "PurchaseOrderId",
                table: "ShipmentInvoicesItems",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentInvoicesItems_PurchaseOrderId",
                table: "ShipmentInvoicesItems",
                column: "PurchaseOrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_ShipmentInvoicesItems_PurchaseOrders_PurchaseOrderId",
                table: "ShipmentInvoicesItems",
                column: "PurchaseOrderId",
                principalTable: "PurchaseOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShipmentInvoicesItems_PurchaseOrders_PurchaseOrderId",
                table: "ShipmentInvoicesItems");

            migrationBuilder.DropIndex(
                name: "IX_ShipmentInvoicesItems_PurchaseOrderId",
                table: "ShipmentInvoicesItems");

            migrationBuilder.DropColumn(
                name: "PurchaseOrderId",
                table: "ShipmentInvoicesItems");

            migrationBuilder.AddColumn<List<Guid>>(
                name: "PurchaseOrderIds",
                table: "ShipmentInvoices",
                type: "uuid[]",
                nullable: true);
        }
    }
}
