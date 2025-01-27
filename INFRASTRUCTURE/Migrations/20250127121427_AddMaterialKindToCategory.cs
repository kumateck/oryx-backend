using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialKindToCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductPackages_PackageTypes_PackageTypeId",
                table: "ProductPackages");

            migrationBuilder.DropIndex(
                name: "IX_ProductPackages_PackageTypeId",
                table: "ProductPackages");

            migrationBuilder.DropColumn(
                name: "PackageTypeId",
                table: "ProductPackages");

            migrationBuilder.DropColumn(
                name: "MaximumStockLevel",
                table: "MaterialCategories");

            migrationBuilder.RenameColumn(
                name: "MinimumStockLevel",
                table: "MaterialCategories",
                newName: "MaterialKind");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MaterialKind",
                table: "MaterialCategories",
                newName: "MinimumStockLevel");

            migrationBuilder.AddColumn<Guid>(
                name: "PackageTypeId",
                table: "ProductPackages",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "MaximumStockLevel",
                table: "MaterialCategories",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ProductPackages_PackageTypeId",
                table: "ProductPackages",
                column: "PackageTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductPackages_PackageTypes_PackageTypeId",
                table: "ProductPackages",
                column: "PackageTypeId",
                principalTable: "PackageTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
