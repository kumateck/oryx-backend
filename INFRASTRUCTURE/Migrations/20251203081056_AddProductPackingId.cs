using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddProductPackingId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProductPackingId",
                table: "FinishedGoodsTransferNotes",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinishedGoodsTransferNotes_ProductPackingId",
                table: "FinishedGoodsTransferNotes",
                column: "ProductPackingId");

            migrationBuilder.AddForeignKey(
                name: "FK_FinishedGoodsTransferNotes_ProductPackings_ProductPackingId",
                table: "FinishedGoodsTransferNotes",
                column: "ProductPackingId",
                principalTable: "ProductPackings",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FinishedGoodsTransferNotes_ProductPackings_ProductPackingId",
                table: "FinishedGoodsTransferNotes");

            migrationBuilder.DropIndex(
                name: "IX_FinishedGoodsTransferNotes_ProductPackingId",
                table: "FinishedGoodsTransferNotes");

            migrationBuilder.DropColumn(
                name: "ProductPackingId",
                table: "FinishedGoodsTransferNotes");
        }
    }
}
