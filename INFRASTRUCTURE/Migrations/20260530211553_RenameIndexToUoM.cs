using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class RenameIndexToUoM : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockAdjustmentLines_UnitOfMeasures_UnitOfMeasureId",
                table: "StockAdjustmentLines");

            migrationBuilder.DropIndex(
                name: "IX_StockAdjustmentLines_UnitOfMeasureId",
                table: "StockAdjustmentLines");

            migrationBuilder.DropColumn(
                name: "UnitOfMeasureId",
                table: "StockAdjustmentLines");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustmentLines_UomId",
                table: "StockAdjustmentLines",
                column: "UomId");

            migrationBuilder.AddForeignKey(
                name: "FK_StockAdjustmentLines_UnitOfMeasures_UomId",
                table: "StockAdjustmentLines",
                column: "UomId",
                principalTable: "UnitOfMeasures",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockAdjustmentLines_UnitOfMeasures_UomId",
                table: "StockAdjustmentLines");

            migrationBuilder.DropIndex(
                name: "IX_StockAdjustmentLines_UomId",
                table: "StockAdjustmentLines");

            migrationBuilder.AddColumn<Guid>(
                name: "UnitOfMeasureId",
                table: "StockAdjustmentLines",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustmentLines_UnitOfMeasureId",
                table: "StockAdjustmentLines",
                column: "UnitOfMeasureId");

            migrationBuilder.AddForeignKey(
                name: "FK_StockAdjustmentLines_UnitOfMeasures_UnitOfMeasureId",
                table: "StockAdjustmentLines",
                column: "UnitOfMeasureId",
                principalTable: "UnitOfMeasures",
                principalColumn: "Id");
        }
    }
}
