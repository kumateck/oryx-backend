using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class ChangeSupplierToVendor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ItemBillingSheets_Suppliers_SupplierId",
                table: "ItemBillingSheets");

            migrationBuilder.DropForeignKey(
                name: "FK_ItemShipmentInvoices_Suppliers_SupplierId",
                table: "ItemShipmentInvoices");

            migrationBuilder.RenameColumn(
                name: "SupplierId",
                table: "ItemShipmentInvoices",
                newName: "VendorId");

            migrationBuilder.RenameIndex(
                name: "IX_ItemShipmentInvoices_SupplierId",
                table: "ItemShipmentInvoices",
                newName: "IX_ItemShipmentInvoices_VendorId");

            migrationBuilder.RenameColumn(
                name: "SupplierId",
                table: "ItemBillingSheets",
                newName: "VendorId");

            migrationBuilder.RenameIndex(
                name: "IX_ItemBillingSheets_SupplierId",
                table: "ItemBillingSheets",
                newName: "IX_ItemBillingSheets_VendorId");

            migrationBuilder.AddColumn<DateTime>(
                name: "ShipmentArrivedAt",
                table: "ItemShipmentInvoices",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddForeignKey(
                name: "FK_ItemBillingSheets_Vendors_VendorId",
                table: "ItemBillingSheets",
                column: "VendorId",
                principalTable: "Vendors",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ItemShipmentInvoices_Vendors_VendorId",
                table: "ItemShipmentInvoices",
                column: "VendorId",
                principalTable: "Vendors",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ItemBillingSheets_Vendors_VendorId",
                table: "ItemBillingSheets");

            migrationBuilder.DropForeignKey(
                name: "FK_ItemShipmentInvoices_Vendors_VendorId",
                table: "ItemShipmentInvoices");

            migrationBuilder.DropColumn(
                name: "ShipmentArrivedAt",
                table: "ItemShipmentInvoices");

            migrationBuilder.RenameColumn(
                name: "VendorId",
                table: "ItemShipmentInvoices",
                newName: "SupplierId");

            migrationBuilder.RenameIndex(
                name: "IX_ItemShipmentInvoices_VendorId",
                table: "ItemShipmentInvoices",
                newName: "IX_ItemShipmentInvoices_SupplierId");

            migrationBuilder.RenameColumn(
                name: "VendorId",
                table: "ItemBillingSheets",
                newName: "SupplierId");

            migrationBuilder.RenameIndex(
                name: "IX_ItemBillingSheets_VendorId",
                table: "ItemBillingSheets",
                newName: "IX_ItemBillingSheets_SupplierId");

            migrationBuilder.AddForeignKey(
                name: "FK_ItemBillingSheets_Suppliers_SupplierId",
                table: "ItemBillingSheets",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ItemShipmentInvoices_Suppliers_SupplierId",
                table: "ItemShipmentInvoices",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id");
        }
    }
}
