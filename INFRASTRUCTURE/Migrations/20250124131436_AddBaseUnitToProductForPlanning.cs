using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddBaseUnitToProductForPlanning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "BasePackingQuantity",
                table: "Products",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "BasePackingUomId",
                table: "Products",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BaseQuantity",
                table: "Products",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "BaseUomId",
                table: "Products",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BaseQuantity",
                table: "ProductPackages",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "BaseUoMId",
                table: "ProductPackages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BaseQuantity",
                table: "BillOfMaterialItems",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "BaseUoMId",
                table: "BillOfMaterialItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_BasePackingUomId",
                table: "Products",
                column: "BasePackingUomId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_BaseUomId",
                table: "Products",
                column: "BaseUomId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPackages_BaseUoMId",
                table: "ProductPackages",
                column: "BaseUoMId");

            migrationBuilder.CreateIndex(
                name: "IX_BillOfMaterialItems_BaseUoMId",
                table: "BillOfMaterialItems",
                column: "BaseUoMId");

            migrationBuilder.AddForeignKey(
                name: "FK_BillOfMaterialItems_UnitOfMeasures_BaseUoMId",
                table: "BillOfMaterialItems",
                column: "BaseUoMId",
                principalTable: "UnitOfMeasures",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductPackages_UnitOfMeasures_BaseUoMId",
                table: "ProductPackages",
                column: "BaseUoMId",
                principalTable: "UnitOfMeasures",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_UnitOfMeasures_BasePackingUomId",
                table: "Products",
                column: "BasePackingUomId",
                principalTable: "UnitOfMeasures",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_UnitOfMeasures_BaseUomId",
                table: "Products",
                column: "BaseUomId",
                principalTable: "UnitOfMeasures",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BillOfMaterialItems_UnitOfMeasures_BaseUoMId",
                table: "BillOfMaterialItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductPackages_UnitOfMeasures_BaseUoMId",
                table: "ProductPackages");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_UnitOfMeasures_BasePackingUomId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_UnitOfMeasures_BaseUomId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_BasePackingUomId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_BaseUomId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_ProductPackages_BaseUoMId",
                table: "ProductPackages");

            migrationBuilder.DropIndex(
                name: "IX_BillOfMaterialItems_BaseUoMId",
                table: "BillOfMaterialItems");

            migrationBuilder.DropColumn(
                name: "BasePackingQuantity",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "BasePackingUomId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "BaseQuantity",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "BaseUomId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "BaseQuantity",
                table: "ProductPackages");

            migrationBuilder.DropColumn(
                name: "BaseUoMId",
                table: "ProductPackages");

            migrationBuilder.DropColumn(
                name: "BaseQuantity",
                table: "BillOfMaterialItems");

            migrationBuilder.DropColumn(
                name: "BaseUoMId",
                table: "BillOfMaterialItems");
        }
    }
}
