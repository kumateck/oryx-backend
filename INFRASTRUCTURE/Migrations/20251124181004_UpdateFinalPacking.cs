using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class UpdateFinalPacking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PackSize",
                table: "FinalPackings");

            migrationBuilder.AddColumn<Guid>(
                name: "ProductPackingId",
                table: "FinalPackings",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinalPackings_ProductPackingId",
                table: "FinalPackings",
                column: "ProductPackingId");

            migrationBuilder.AddForeignKey(
                name: "FK_FinalPackings_ProductPackings_ProductPackingId",
                table: "FinalPackings",
                column: "ProductPackingId",
                principalTable: "ProductPackings",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FinalPackings_ProductPackings_ProductPackingId",
                table: "FinalPackings");

            migrationBuilder.DropIndex(
                name: "IX_FinalPackings_ProductPackingId",
                table: "FinalPackings");

            migrationBuilder.DropColumn(
                name: "ProductPackingId",
                table: "FinalPackings");

            migrationBuilder.AddColumn<decimal>(
                name: "PackSize",
                table: "FinalPackings",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
