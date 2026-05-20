using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddedProductToStockAdjustment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FinishedGoodsTransferNoteId",
                table: "StockAdjustmentLines",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FinishedGoodsTransferNoteId",
                table: "InventoryLedgers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustmentLines_FinishedGoodsTransferNoteId",
                table: "StockAdjustmentLines",
                column: "FinishedGoodsTransferNoteId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLedgers_FinishedGoodsTransferNoteId",
                table: "InventoryLedgers",
                column: "FinishedGoodsTransferNoteId");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryLedgers_FinishedGoodsTransferNotes_FinishedGoodsTr~",
                table: "InventoryLedgers",
                column: "FinishedGoodsTransferNoteId",
                principalTable: "FinishedGoodsTransferNotes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StockAdjustmentLines_FinishedGoodsTransferNotes_FinishedGoo~",
                table: "StockAdjustmentLines",
                column: "FinishedGoodsTransferNoteId",
                principalTable: "FinishedGoodsTransferNotes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryLedgers_FinishedGoodsTransferNotes_FinishedGoodsTr~",
                table: "InventoryLedgers");

            migrationBuilder.DropForeignKey(
                name: "FK_StockAdjustmentLines_FinishedGoodsTransferNotes_FinishedGoo~",
                table: "StockAdjustmentLines");

            migrationBuilder.DropIndex(
                name: "IX_StockAdjustmentLines_FinishedGoodsTransferNoteId",
                table: "StockAdjustmentLines");

            migrationBuilder.DropIndex(
                name: "IX_InventoryLedgers_FinishedGoodsTransferNoteId",
                table: "InventoryLedgers");

            migrationBuilder.DropColumn(
                name: "FinishedGoodsTransferNoteId",
                table: "StockAdjustmentLines");

            migrationBuilder.DropColumn(
                name: "FinishedGoodsTransferNoteId",
                table: "InventoryLedgers");
        }
    }
}
