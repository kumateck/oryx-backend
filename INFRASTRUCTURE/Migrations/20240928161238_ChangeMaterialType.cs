using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class ChangeMaterialType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductPackages_MaterialTypes_MaterialTypeId",
                table: "ProductPackages");

            migrationBuilder.RenameColumn(
                name: "MaterialTypeId",
                table: "ProductPackages",
                newName: "MaterialId");

            migrationBuilder.RenameIndex(
                name: "IX_ProductPackages_MaterialTypeId",
                table: "ProductPackages",
                newName: "IX_ProductPackages_MaterialId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductPackages_Materials_MaterialId",
                table: "ProductPackages",
                column: "MaterialId",
                principalTable: "Materials",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductPackages_Materials_MaterialId",
                table: "ProductPackages");

            migrationBuilder.RenameColumn(
                name: "MaterialId",
                table: "ProductPackages",
                newName: "MaterialTypeId");

            migrationBuilder.RenameIndex(
                name: "IX_ProductPackages_MaterialId",
                table: "ProductPackages",
                newName: "IX_ProductPackages_MaterialTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductPackages_MaterialTypes_MaterialTypeId",
                table: "ProductPackages",
                column: "MaterialTypeId",
                principalTable: "MaterialTypes",
                principalColumn: "Id");
        }
    }
}
