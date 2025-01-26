using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class LinkSourceRequisitionToPO : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BillOfMaterialItems_UnitOfMeasures_UoMId",
                table: "BillOfMaterialItems");

            migrationBuilder.DropIndex(
                name: "IX_BillOfMaterialItems_UoMId",
                table: "BillOfMaterialItems");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "BillOfMaterialItems");

            migrationBuilder.DropColumn(
                name: "UoMId",
                table: "BillOfMaterialItems");

            migrationBuilder.AddColumn<Guid>(
                name: "SourceRequisitionId",
                table: "SupplierQuotations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceRequisitionId",
                table: "PurchaseOrders",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierQuotations_SourceRequisitionId",
                table: "SupplierQuotations",
                column: "SourceRequisitionId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_SourceRequisitionId",
                table: "PurchaseOrders",
                column: "SourceRequisitionId");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_SourceRequisitions_SourceRequisitionId",
                table: "PurchaseOrders",
                column: "SourceRequisitionId",
                principalTable: "SourceRequisitions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierQuotations_SourceRequisitions_SourceRequisitionId",
                table: "SupplierQuotations",
                column: "SourceRequisitionId",
                principalTable: "SourceRequisitions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_SourceRequisitions_SourceRequisitionId",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplierQuotations_SourceRequisitions_SourceRequisitionId",
                table: "SupplierQuotations");

            migrationBuilder.DropIndex(
                name: "IX_SupplierQuotations_SourceRequisitionId",
                table: "SupplierQuotations");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_SourceRequisitionId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "SourceRequisitionId",
                table: "SupplierQuotations");

            migrationBuilder.DropColumn(
                name: "SourceRequisitionId",
                table: "PurchaseOrders");

            migrationBuilder.AddColumn<decimal>(
                name: "Quantity",
                table: "BillOfMaterialItems",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "UoMId",
                table: "BillOfMaterialItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BillOfMaterialItems_UoMId",
                table: "BillOfMaterialItems",
                column: "UoMId");

            migrationBuilder.AddForeignKey(
                name: "FK_BillOfMaterialItems_UnitOfMeasures_UoMId",
                table: "BillOfMaterialItems",
                column: "UoMId",
                principalTable: "UnitOfMeasures",
                principalColumn: "Id");
        }
    }
}
