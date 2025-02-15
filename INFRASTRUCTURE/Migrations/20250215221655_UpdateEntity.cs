using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialBatchEvents_WarehouseLocations_ConsumedLocationId",
                table: "MaterialBatchEvents");

            migrationBuilder.RenameColumn(
                name: "ConsumedLocationId",
                table: "MaterialBatchEvents",
                newName: "ConsumptionWarehouseId");

            migrationBuilder.RenameIndex(
                name: "IX_MaterialBatchEvents_ConsumedLocationId",
                table: "MaterialBatchEvents",
                newName: "IX_MaterialBatchEvents_ConsumptionWarehouseId");

            migrationBuilder.AddColumn<int>(
                name: "Order",
                table: "Operations",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialBatchEvents_Warehouses_ConsumptionWarehouseId",
                table: "MaterialBatchEvents");

            migrationBuilder.DropColumn(
                name: "Order",
                table: "Operations");

            migrationBuilder.RenameColumn(
                name: "ConsumptionWarehouseId",
                table: "MaterialBatchEvents",
                newName: "ConsumedLocationId");

            migrationBuilder.RenameIndex(
                name: "IX_MaterialBatchEvents_ConsumptionWarehouseId",
                table: "MaterialBatchEvents",
                newName: "IX_MaterialBatchEvents_ConsumedLocationId");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialBatchEvents_WarehouseLocations_ConsumedLocationId",
                table: "MaterialBatchEvents",
                column: "ConsumedLocationId",
                principalTable: "WarehouseLocations",
                principalColumn: "Id");
        }
    }
}
