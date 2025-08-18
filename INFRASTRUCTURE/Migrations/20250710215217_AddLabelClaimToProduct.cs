using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddLabelClaimToProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShelfMaterialBatches_UnitOfMeasures_UomId",
                table: "ShelfMaterialBatches");

            migrationBuilder.RenameColumn(
                name: "UomId",
                table: "ShelfMaterialBatches",
                newName: "UoMId");

            migrationBuilder.RenameIndex(
                name: "IX_ShelfMaterialBatches_UomId",
                table: "ShelfMaterialBatches",
                newName: "IX_ShelfMaterialBatches_UoMId");

            migrationBuilder.AddColumn<string>(
                name: "LabelClaim",
                table: "Products",
                type: "character varying(1000000)",
                maxLength: 1000000,
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ShelfMaterialBatches_UnitOfMeasures_UoMId",
                table: "ShelfMaterialBatches",
                column: "UoMId",
                principalTable: "UnitOfMeasures",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShelfMaterialBatches_UnitOfMeasures_UoMId",
                table: "ShelfMaterialBatches");

            migrationBuilder.DropColumn(
                name: "LabelClaim",
                table: "Products");

            migrationBuilder.RenameColumn(
                name: "UoMId",
                table: "ShelfMaterialBatches",
                newName: "UomId");

            migrationBuilder.RenameIndex(
                name: "IX_ShelfMaterialBatches_UoMId",
                table: "ShelfMaterialBatches",
                newName: "IX_ShelfMaterialBatches_UomId");

            migrationBuilder.AddForeignKey(
                name: "FK_ShelfMaterialBatches_UnitOfMeasures_UomId",
                table: "ShelfMaterialBatches",
                column: "UomId",
                principalTable: "UnitOfMeasures",
                principalColumn: "Id");
        }
    }
}
