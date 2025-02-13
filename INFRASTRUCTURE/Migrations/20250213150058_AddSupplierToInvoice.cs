using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierToInvoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SupplierId",
                table: "ShipmentInvoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentInvoices_SupplierId",
                table: "ShipmentInvoices",
                column: "SupplierId");

            migrationBuilder.AddForeignKey(
                name: "FK_ShipmentInvoices_Suppliers_SupplierId",
                table: "ShipmentInvoices",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id");
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
