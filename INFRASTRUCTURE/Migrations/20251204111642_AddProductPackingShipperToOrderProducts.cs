using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddProductPackingShipperToOrderProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Loose",
                table: "ProductionOrderProducts",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "ProductPackingId",
                table: "ProductionOrderProducts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Shippers",
                table: "ProductionOrderProducts",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrderProducts_ProductPackingId",
                table: "ProductionOrderProducts",
                column: "ProductPackingId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionOrderProducts_ProductPackings_ProductPackingId",
                table: "ProductionOrderProducts",
                column: "ProductPackingId",
                principalTable: "ProductPackings",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductionOrderProducts_ProductPackings_ProductPackingId",
                table: "ProductionOrderProducts");

            migrationBuilder.DropIndex(
                name: "IX_ProductionOrderProducts_ProductPackingId",
                table: "ProductionOrderProducts");

            migrationBuilder.DropColumn(
                name: "Loose",
                table: "ProductionOrderProducts");

            migrationBuilder.DropColumn(
                name: "ProductPackingId",
                table: "ProductionOrderProducts");

            migrationBuilder.DropColumn(
                name: "Shippers",
                table: "ProductionOrderProducts");
        }
    }
}
