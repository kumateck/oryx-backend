using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class RemoveProductPackageFromFGTN : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FinishedGoodsTransferNotes_PackageStyles_PackageStyleId",
                table: "FinishedGoodsTransferNotes");

            migrationBuilder.DropIndex(
                name: "IX_FinishedGoodsTransferNotes_PackageStyleId",
                table: "FinishedGoodsTransferNotes");

            migrationBuilder.DropColumn(
                name: "PackageStyleId",
                table: "FinishedGoodsTransferNotes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PackageStyleId",
                table: "FinishedGoodsTransferNotes",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinishedGoodsTransferNotes_PackageStyleId",
                table: "FinishedGoodsTransferNotes",
                column: "PackageStyleId");

            migrationBuilder.AddForeignKey(
                name: "FK_FinishedGoodsTransferNotes_PackageStyles_PackageStyleId",
                table: "FinishedGoodsTransferNotes",
                column: "PackageStyleId",
                principalTable: "PackageStyles",
                principalColumn: "Id");
        }
    }
}
