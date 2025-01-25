using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class RemoveComponentProductFromBomItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BillOfMaterialItems_Materials_ComponentMaterialId",
                table: "BillOfMaterialItems");

            migrationBuilder.DropForeignKey(
                name: "FK_BillOfMaterialItems_Products_ComponentProductId",
                table: "BillOfMaterialItems");

            migrationBuilder.DropIndex(
                name: "IX_BillOfMaterialItems_ComponentMaterialId",
                table: "BillOfMaterialItems");

            migrationBuilder.DropColumn(
                name: "ComponentMaterialId",
                table: "BillOfMaterialItems");

            migrationBuilder.RenameColumn(
                name: "ComponentProductId",
                table: "BillOfMaterialItems",
                newName: "MaterialId");

            migrationBuilder.RenameIndex(
                name: "IX_BillOfMaterialItems_ComponentProductId",
                table: "BillOfMaterialItems",
                newName: "IX_BillOfMaterialItems_MaterialId");

            migrationBuilder.AddForeignKey(
                name: "FK_BillOfMaterialItems_Materials_MaterialId",
                table: "BillOfMaterialItems",
                column: "MaterialId",
                principalTable: "Materials",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BillOfMaterialItems_Materials_MaterialId",
                table: "BillOfMaterialItems");

            migrationBuilder.RenameColumn(
                name: "MaterialId",
                table: "BillOfMaterialItems",
                newName: "ComponentProductId");

            migrationBuilder.RenameIndex(
                name: "IX_BillOfMaterialItems_MaterialId",
                table: "BillOfMaterialItems",
                newName: "IX_BillOfMaterialItems_ComponentProductId");

            migrationBuilder.AddColumn<Guid>(
                name: "ComponentMaterialId",
                table: "BillOfMaterialItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BillOfMaterialItems_ComponentMaterialId",
                table: "BillOfMaterialItems",
                column: "ComponentMaterialId");

            migrationBuilder.AddForeignKey(
                name: "FK_BillOfMaterialItems_Materials_ComponentMaterialId",
                table: "BillOfMaterialItems",
                column: "ComponentMaterialId",
                principalTable: "Materials",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BillOfMaterialItems_Products_ComponentProductId",
                table: "BillOfMaterialItems",
                column: "ComponentProductId",
                principalTable: "Products",
                principalColumn: "Id");
        }
    }
}
