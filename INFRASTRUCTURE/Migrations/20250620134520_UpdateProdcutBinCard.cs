using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class UpdateProdcutBinCard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductBinCardInformation_Products_ProductId",
                table: "ProductBinCardInformation");

            migrationBuilder.DropIndex(
                name: "IX_ProductBinCardInformation_ProductId",
                table: "ProductBinCardInformation");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "ProductBinCardInformation");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProductId",
                table: "ProductBinCardInformation",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductBinCardInformation_ProductId",
                table: "ProductBinCardInformation",
                column: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductBinCardInformation_Products_ProductId",
                table: "ProductBinCardInformation",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id");
        }
    }
}
