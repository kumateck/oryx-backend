using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddProductPackingIdToAllocateProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Function",
                table: "BillOfMaterialItems");

            migrationBuilder.AddColumn<Guid>(
                name: "ProductPackingId",
                table: "AllocateProductionOrderProduct",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AllocateProductionOrderProduct_ProductPackingId",
                table: "AllocateProductionOrderProduct",
                column: "ProductPackingId");

            migrationBuilder.AddForeignKey(
                name: "FK_AllocateProductionOrderProduct_ProductPackings_ProductPacki~",
                table: "AllocateProductionOrderProduct",
                column: "ProductPackingId",
                principalTable: "ProductPackings",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AllocateProductionOrderProduct_ProductPackings_ProductPacki~",
                table: "AllocateProductionOrderProduct");

            migrationBuilder.DropIndex(
                name: "IX_AllocateProductionOrderProduct_ProductPackingId",
                table: "AllocateProductionOrderProduct");

            migrationBuilder.DropColumn(
                name: "ProductPackingId",
                table: "AllocateProductionOrderProduct");

            migrationBuilder.AddColumn<string>(
                name: "Function",
                table: "BillOfMaterialItems",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);
        }
    }
}
