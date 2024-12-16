using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePurchaseOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrderItem_UnitOfMeasures_UomId",
                table: "PurchaseOrderItem");

            migrationBuilder.RenameColumn(
                name: "UomId",
                table: "PurchaseOrderItem",
                newName: "UoMId");

            migrationBuilder.RenameIndex(
                name: "IX_PurchaseOrderItem_UomId",
                table: "PurchaseOrderItem",
                newName: "IX_PurchaseOrderItem_UoMId");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ExpectedDeliveryDate",
                table: "PurchaseOrders",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "PurchaseOrders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrderItem_UnitOfMeasures_UoMId",
                table: "PurchaseOrderItem",
                column: "UoMId",
                principalTable: "UnitOfMeasures",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrderItem_UnitOfMeasures_UoMId",
                table: "PurchaseOrderItem");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "PurchaseOrders");

            migrationBuilder.RenameColumn(
                name: "UoMId",
                table: "PurchaseOrderItem",
                newName: "UomId");

            migrationBuilder.RenameIndex(
                name: "IX_PurchaseOrderItem_UoMId",
                table: "PurchaseOrderItem",
                newName: "IX_PurchaseOrderItem_UomId");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ExpectedDeliveryDate",
                table: "PurchaseOrders",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrderItem_UnitOfMeasures_UomId",
                table: "PurchaseOrderItem",
                column: "UomId",
                principalTable: "UnitOfMeasures",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
