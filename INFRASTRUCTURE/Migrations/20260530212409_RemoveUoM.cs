using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUoM : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockAdjustmentLines_UnitOfMeasures_UoMId",
                table: "StockAdjustmentLines");

            migrationBuilder.DropIndex(
                name: "IX_StockAdjustmentLines_UoMId",
                table: "StockAdjustmentLines");

            migrationBuilder.DropColumn(
                name: "UoMId",
                table: "StockAdjustmentLines");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UoMId",
                table: "StockAdjustmentLines",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustmentLines_UoMId",
                table: "StockAdjustmentLines",
                column: "UoMId");

            migrationBuilder.AddForeignKey(
                name: "FK_StockAdjustmentLines_UnitOfMeasures_UoMId",
                table: "StockAdjustmentLines",
                column: "UoMId",
                principalTable: "UnitOfMeasures",
                principalColumn: "Id");
        }
    }
}
