using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class Updated_BinCardInformation_MaterialBatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BatchNumber",
                table: "BinCardInformation");

            migrationBuilder.DropColumn(
                name: "ExpiryDate",
                table: "BinCardInformation");

            migrationBuilder.DropColumn(
                name: "ManufacturingDate",
                table: "BinCardInformation");

            migrationBuilder.DropColumn(
                name: "ProductName",
                table: "BinCardInformation");

            migrationBuilder.AddColumn<Guid>(
                name: "MaterialBatchId",
                table: "BinCardInformation",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProductId",
                table: "BinCardInformation",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BinCardInformation_MaterialBatchId",
                table: "BinCardInformation",
                column: "MaterialBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_BinCardInformation_ProductId",
                table: "BinCardInformation",
                column: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_BinCardInformation_MaterialBatches_MaterialBatchId",
                table: "BinCardInformation",
                column: "MaterialBatchId",
                principalTable: "MaterialBatches",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BinCardInformation_Products_ProductId",
                table: "BinCardInformation",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BinCardInformation_MaterialBatches_MaterialBatchId",
                table: "BinCardInformation");

            migrationBuilder.DropForeignKey(
                name: "FK_BinCardInformation_Products_ProductId",
                table: "BinCardInformation");

            migrationBuilder.DropIndex(
                name: "IX_BinCardInformation_MaterialBatchId",
                table: "BinCardInformation");

            migrationBuilder.DropIndex(
                name: "IX_BinCardInformation_ProductId",
                table: "BinCardInformation");

            migrationBuilder.DropColumn(
                name: "MaterialBatchId",
                table: "BinCardInformation");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "BinCardInformation");

            migrationBuilder.AddColumn<string>(
                name: "BatchNumber",
                table: "BinCardInformation",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiryDate",
                table: "BinCardInformation",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ManufacturingDate",
                table: "BinCardInformation",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductName",
                table: "BinCardInformation",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }
    }
}
