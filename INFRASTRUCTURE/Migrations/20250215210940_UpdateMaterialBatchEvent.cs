using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class UpdateMaterialBatchEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialBatchEvents_WarehouseLocations_ConsumedLocationId",
                table: "MaterialBatchEvents");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialBatchEvents_Warehouses_ConsumedLocationId",
                table: "MaterialBatchEvents",
                column: "ConsumedLocationId",
                principalTable: "Warehouses",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialBatchEvents_Warehouses_ConsumedLocationId",
                table: "MaterialBatchEvents");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialBatchEvents_WarehouseLocations_ConsumedLocationId",
                table: "MaterialBatchEvents",
                column: "ConsumedLocationId",
                principalTable: "WarehouseLocations",
                principalColumn: "Id");
        }
    }
}
