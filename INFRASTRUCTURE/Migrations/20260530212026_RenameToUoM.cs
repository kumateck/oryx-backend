using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class RenameToUoM : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockAdjustmentLines_UnitOfMeasures_UomId",
                table: "StockAdjustmentLines");

            migrationBuilder.RenameColumn(
                name: "UomId",
                table: "StockAdjustmentLines",
                newName: "UoMId");

            migrationBuilder.RenameIndex(
                name: "IX_StockAdjustmentLines_UomId",
                table: "StockAdjustmentLines",
                newName: "IX_StockAdjustmentLines_UoMId");

            migrationBuilder.AddForeignKey(
                name: "FK_StockAdjustmentLines_UnitOfMeasures_UoMId",
                table: "StockAdjustmentLines",
                column: "UoMId",
                principalTable: "UnitOfMeasures",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockAdjustmentLines_UnitOfMeasures_UoMId",
                table: "StockAdjustmentLines");

            migrationBuilder.RenameColumn(
                name: "UoMId",
                table: "StockAdjustmentLines",
                newName: "UomId");

            migrationBuilder.RenameIndex(
                name: "IX_StockAdjustmentLines_UoMId",
                table: "StockAdjustmentLines",
                newName: "IX_StockAdjustmentLines_UomId");

            migrationBuilder.AddForeignKey(
                name: "FK_StockAdjustmentLines_UnitOfMeasures_UomId",
                table: "StockAdjustmentLines",
                column: "UomId",
                principalTable: "UnitOfMeasures",
                principalColumn: "Id");
        }
    }
}
