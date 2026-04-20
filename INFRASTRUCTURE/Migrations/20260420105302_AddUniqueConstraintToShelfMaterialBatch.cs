using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueConstraintToShelfMaterialBatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ShelfMaterialBatches_WarehouseLocationShelfId",
                table: "ShelfMaterialBatches");

            migrationBuilder.CreateIndex(
                name: "IX_ShelfMaterialBatch_Unique_Shelf_Batch",
                table: "ShelfMaterialBatches",
                columns: new[] { "WarehouseLocationShelfId", "MaterialBatchId" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ShelfMaterialBatch_Unique_Shelf_Batch",
                table: "ShelfMaterialBatches");

            migrationBuilder.CreateIndex(
                name: "IX_ShelfMaterialBatches_WarehouseLocationShelfId",
                table: "ShelfMaterialBatches",
                column: "WarehouseLocationShelfId");
        }
    }
}
