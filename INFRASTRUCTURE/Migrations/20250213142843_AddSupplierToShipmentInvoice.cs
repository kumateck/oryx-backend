using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierToShipmentInvoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SupplierId",
                table: "ShipmentInvoices",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentInvoices_SupplierId",
                table: "ShipmentInvoices",
                column: "SupplierId");

            migrationBuilder.AddForeignKey(
                name: "FK_ShipmentInvoices_Suppliers_SupplierId",
                table: "ShipmentInvoices",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShipmentInvoices_Suppliers_SupplierId",
                table: "ShipmentInvoices");

            migrationBuilder.DropIndex(
                name: "IX_ShipmentInvoices_SupplierId",
                table: "ShipmentInvoices");

            migrationBuilder.DropColumn(
                name: "SupplierId",
                table: "ShipmentInvoices");
        }
    }
}
