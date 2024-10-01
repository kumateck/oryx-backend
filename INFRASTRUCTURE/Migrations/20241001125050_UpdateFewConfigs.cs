using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class UpdateFewConfigs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductPackages_Materials_MaterialId",
                table: "ProductPackages");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductPackages_PackageTypes_PackageTypeId",
                table: "ProductPackages");

            migrationBuilder.AddColumn<Guid>(
                name: "ProductId",
                table: "Routes",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<Guid>(
                name: "PackageTypeId",
                table: "ProductPackages",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "MaterialId",
                table: "ProductPackages",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Routes_ProductId",
                table: "Routes",
                column: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductPackages_Materials_MaterialId",
                table: "ProductPackages",
                column: "MaterialId",
                principalTable: "Materials",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductPackages_PackageTypes_PackageTypeId",
                table: "ProductPackages",
                column: "PackageTypeId",
                principalTable: "PackageTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Routes_Products_ProductId",
                table: "Routes",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductPackages_Materials_MaterialId",
                table: "ProductPackages");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductPackages_PackageTypes_PackageTypeId",
                table: "ProductPackages");

            migrationBuilder.DropForeignKey(
                name: "FK_Routes_Products_ProductId",
                table: "Routes");

            migrationBuilder.DropIndex(
                name: "IX_Routes_ProductId",
                table: "Routes");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "Routes");

            migrationBuilder.AlterColumn<Guid>(
                name: "PackageTypeId",
                table: "ProductPackages",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "MaterialId",
                table: "ProductPackages",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductPackages_Materials_MaterialId",
                table: "ProductPackages",
                column: "MaterialId",
                principalTable: "Materials",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductPackages_PackageTypes_PackageTypeId",
                table: "ProductPackages",
                column: "PackageTypeId",
                principalTable: "PackageTypes",
                principalColumn: "Id");
        }
    }
}
