using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class MajorUpdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SentQuotationRequestAt",
                table: "SourceRequisitionItemSuppliers");

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "SupplierQuotationItems",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<List<Guid>>(
                name: "RequisitionIds",
                table: "SourceRequisitions",
                type: "uuid[]",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SentQuotationRequestAt",
                table: "SourceRequisitions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SupplierId",
                table: "SourceRequisitions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "SourceRequisitionItems",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "ProductionSchedules",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "ProductionScheduleItems",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "MaterialBatchMovements",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "MaterialBatchEvents",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalQuantity",
                table: "MaterialBatches",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<decimal>(
                name: "ConsumedQuantity",
                table: "MaterialBatches",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "CompletedRequisitionItems",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<Guid>(
                name: "UoMId",
                table: "CompletedRequisitionItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "BillOfMaterialItems",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateIndex(
                name: "IX_SourceRequisitions_SupplierId",
                table: "SourceRequisitions",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_CompletedRequisitionItems_UoMId",
                table: "CompletedRequisitionItems",
                column: "UoMId");

            migrationBuilder.AddForeignKey(
                name: "FK_CompletedRequisitionItems_UnitOfMeasures_UoMId",
                table: "CompletedRequisitionItems",
                column: "UoMId",
                principalTable: "UnitOfMeasures",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SourceRequisitions_Suppliers_SupplierId",
                table: "SourceRequisitions",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CompletedRequisitionItems_UnitOfMeasures_UoMId",
                table: "CompletedRequisitionItems");

            migrationBuilder.DropForeignKey(
                name: "FK_SourceRequisitions_Suppliers_SupplierId",
                table: "SourceRequisitions");

            migrationBuilder.DropIndex(
                name: "IX_SourceRequisitions_SupplierId",
                table: "SourceRequisitions");

            migrationBuilder.DropIndex(
                name: "IX_CompletedRequisitionItems_UoMId",
                table: "CompletedRequisitionItems");

            migrationBuilder.DropColumn(
                name: "RequisitionIds",
                table: "SourceRequisitions");

            migrationBuilder.DropColumn(
                name: "SentQuotationRequestAt",
                table: "SourceRequisitions");

            migrationBuilder.DropColumn(
                name: "SupplierId",
                table: "SourceRequisitions");

            migrationBuilder.DropColumn(
                name: "UoMId",
                table: "CompletedRequisitionItems");

            migrationBuilder.AlterColumn<int>(
                name: "Quantity",
                table: "SupplierQuotationItems",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<DateTime>(
                name: "SentQuotationRequestAt",
                table: "SourceRequisitionItemSuppliers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Quantity",
                table: "SourceRequisitionItems",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<int>(
                name: "Quantity",
                table: "ProductionSchedules",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<int>(
                name: "Quantity",
                table: "ProductionScheduleItems",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<int>(
                name: "Quantity",
                table: "MaterialBatchMovements",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<int>(
                name: "Quantity",
                table: "MaterialBatchEvents",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<int>(
                name: "TotalQuantity",
                table: "MaterialBatches",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<int>(
                name: "ConsumedQuantity",
                table: "MaterialBatches",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<int>(
                name: "Quantity",
                table: "CompletedRequisitionItems",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<int>(
                name: "Quantity",
                table: "BillOfMaterialItems",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");
        }
    }
}
