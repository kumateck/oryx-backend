using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddManufacturerToShipmentInvoiceItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ManufacturerId",
                table: "ShipmentInvoiceItem",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentInvoiceItem_ManufacturerId",
                table: "ShipmentInvoiceItem",
                column: "ManufacturerId");

            migrationBuilder.AddForeignKey(
                name: "FK_ShipmentInvoiceItem_Manufacturers_ManufacturerId",
                table: "ShipmentInvoiceItem",
                column: "ManufacturerId",
                principalTable: "Manufacturers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShipmentInvoiceItem_Manufacturers_ManufacturerId",
                table: "ShipmentInvoiceItem");

            migrationBuilder.DropIndex(
                name: "IX_ShipmentInvoiceItem_ManufacturerId",
                table: "ShipmentInvoiceItem");

            migrationBuilder.DropColumn(
                name: "ManufacturerId",
                table: "ShipmentInvoiceItem");
        }
    }
}
