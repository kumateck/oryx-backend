using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class Added_DistributedStockTransferBatches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "StockTransferSourceId",
                table: "MaterialBatches",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WarehouseArrivalLocationId",
                table: "MaterialBatches",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaterialBatches_StockTransferSourceId",
                table: "MaterialBatches",
                column: "StockTransferSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialBatches_WarehouseArrivalLocationId",
                table: "MaterialBatches",
                column: "WarehouseArrivalLocationId");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialBatches_StockTransferSources_StockTransferSourceId",
                table: "MaterialBatches",
                column: "StockTransferSourceId",
                principalTable: "StockTransferSources",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialBatches_WarehouseArrivalLocations_WarehouseArrivalL~",
                table: "MaterialBatches",
                column: "WarehouseArrivalLocationId",
                principalTable: "WarehouseArrivalLocations",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialBatches_StockTransferSources_StockTransferSourceId",
                table: "MaterialBatches");

            migrationBuilder.DropForeignKey(
                name: "FK_MaterialBatches_WarehouseArrivalLocations_WarehouseArrivalL~",
                table: "MaterialBatches");

            migrationBuilder.DropIndex(
                name: "IX_MaterialBatches_StockTransferSourceId",
                table: "MaterialBatches");

            migrationBuilder.DropIndex(
                name: "IX_MaterialBatches_WarehouseArrivalLocationId",
                table: "MaterialBatches");

            migrationBuilder.DropColumn(
                name: "StockTransferSourceId",
                table: "MaterialBatches");

            migrationBuilder.DropColumn(
                name: "WarehouseArrivalLocationId",
                table: "MaterialBatches");
        }
    }
}
