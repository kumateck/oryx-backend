using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class UpdateStockTransferSourceOnMaterialBatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialBatches_StockTransferSources_StockTransferSourceId",
                table: "MaterialBatches");

            migrationBuilder.RenameColumn(
                name: "StockTransferSourceId",
                table: "MaterialBatches",
                newName: "StockTransferId");

            migrationBuilder.RenameIndex(
                name: "IX_MaterialBatches_StockTransferSourceId",
                table: "MaterialBatches",
                newName: "IX_MaterialBatches_StockTransferId");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialBatches_StockTransfers_StockTransferId",
                table: "MaterialBatches",
                column: "StockTransferId",
                principalTable: "StockTransfers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialBatches_StockTransfers_StockTransferId",
                table: "MaterialBatches");

            migrationBuilder.RenameColumn(
                name: "StockTransferId",
                table: "MaterialBatches",
                newName: "StockTransferSourceId");

            migrationBuilder.RenameIndex(
                name: "IX_MaterialBatches_StockTransferId",
                table: "MaterialBatches",
                newName: "IX_MaterialBatches_StockTransferSourceId");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialBatches_StockTransferSources_StockTransferSourceId",
                table: "MaterialBatches",
                column: "StockTransferSourceId",
                principalTable: "StockTransferSources",
                principalColumn: "Id");
        }
    }
}
