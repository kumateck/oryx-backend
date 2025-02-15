using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class Created_Checklist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ManufacturerId",
                table: "DistributedRequisitionMaterials",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ShipmentInvoiceId",
                table: "DistributedRequisitionMaterials",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ShipmentInvoiceItemId",
                table: "DistributedRequisitionMaterials",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "SupplierId",
                table: "DistributedRequisitionMaterials",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_DistributedRequisitionMaterials_ManufacturerId",
                table: "DistributedRequisitionMaterials",
                column: "ManufacturerId");

            migrationBuilder.CreateIndex(
                name: "IX_DistributedRequisitionMaterials_ShipmentInvoiceId",
                table: "DistributedRequisitionMaterials",
                column: "ShipmentInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_DistributedRequisitionMaterials_ShipmentInvoiceItemId",
                table: "DistributedRequisitionMaterials",
                column: "ShipmentInvoiceItemId");

            migrationBuilder.CreateIndex(
                name: "IX_DistributedRequisitionMaterials_SupplierId",
                table: "DistributedRequisitionMaterials",
                column: "SupplierId");

            migrationBuilder.AddForeignKey(
                name: "FK_DistributedRequisitionMaterials_Manufacturers_ManufacturerId",
                table: "DistributedRequisitionMaterials",
                column: "ManufacturerId",
                principalTable: "Manufacturers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DistributedRequisitionMaterials_ShipmentInvoicesItems_Shipm~",
                table: "DistributedRequisitionMaterials",
                column: "ShipmentInvoiceId",
                principalTable: "ShipmentInvoicesItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DistributedRequisitionMaterials_ShipmentInvoicesItems_Ship~1",
                table: "DistributedRequisitionMaterials",
                column: "ShipmentInvoiceItemId",
                principalTable: "ShipmentInvoicesItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DistributedRequisitionMaterials_Suppliers_SupplierId",
                table: "DistributedRequisitionMaterials",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DistributedRequisitionMaterials_Manufacturers_ManufacturerId",
                table: "DistributedRequisitionMaterials");

            migrationBuilder.DropForeignKey(
                name: "FK_DistributedRequisitionMaterials_ShipmentInvoicesItems_Shipm~",
                table: "DistributedRequisitionMaterials");

            migrationBuilder.DropForeignKey(
                name: "FK_DistributedRequisitionMaterials_ShipmentInvoicesItems_Ship~1",
                table: "DistributedRequisitionMaterials");

            migrationBuilder.DropForeignKey(
                name: "FK_DistributedRequisitionMaterials_Suppliers_SupplierId",
                table: "DistributedRequisitionMaterials");

            migrationBuilder.DropIndex(
                name: "IX_DistributedRequisitionMaterials_ManufacturerId",
                table: "DistributedRequisitionMaterials");

            migrationBuilder.DropIndex(
                name: "IX_DistributedRequisitionMaterials_ShipmentInvoiceId",
                table: "DistributedRequisitionMaterials");

            migrationBuilder.DropIndex(
                name: "IX_DistributedRequisitionMaterials_ShipmentInvoiceItemId",
                table: "DistributedRequisitionMaterials");

            migrationBuilder.DropIndex(
                name: "IX_DistributedRequisitionMaterials_SupplierId",
                table: "DistributedRequisitionMaterials");

            migrationBuilder.DropColumn(
                name: "ManufacturerId",
                table: "DistributedRequisitionMaterials");

            migrationBuilder.DropColumn(
                name: "ShipmentInvoiceId",
                table: "DistributedRequisitionMaterials");

            migrationBuilder.DropColumn(
                name: "ShipmentInvoiceItemId",
                table: "DistributedRequisitionMaterials");

            migrationBuilder.DropColumn(
                name: "SupplierId",
                table: "DistributedRequisitionMaterials");
        }
    }
}
