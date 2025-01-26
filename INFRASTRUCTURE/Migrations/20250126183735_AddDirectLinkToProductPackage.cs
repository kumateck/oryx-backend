using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddDirectLinkToProductPackage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DirectLinkMaterialId",
                table: "ProductPackages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitCapacity",
                table: "ProductPackages",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_ProductPackages_DirectLinkMaterialId",
                table: "ProductPackages",
                column: "DirectLinkMaterialId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductPackages_Materials_DirectLinkMaterialId",
                table: "ProductPackages",
                column: "DirectLinkMaterialId",
                principalTable: "Materials",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductPackages_Materials_DirectLinkMaterialId",
                table: "ProductPackages");

            migrationBuilder.DropIndex(
                name: "IX_ProductPackages_DirectLinkMaterialId",
                table: "ProductPackages");

            migrationBuilder.DropColumn(
                name: "DirectLinkMaterialId",
                table: "ProductPackages");

            migrationBuilder.DropColumn(
                name: "UnitCapacity",
                table: "ProductPackages");
        }
    }
}
