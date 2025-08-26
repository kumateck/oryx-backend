using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddProductPackingIdToBpr : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProductPackingId",
                table: "BatchPackagingRecords",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BatchPackagingRecords_ProductPackingId",
                table: "BatchPackagingRecords",
                column: "ProductPackingId");

            migrationBuilder.AddForeignKey(
                name: "FK_BatchPackagingRecords_ProductPackings_ProductPackingId",
                table: "BatchPackagingRecords",
                column: "ProductPackingId",
                principalTable: "ProductPackings",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BatchPackagingRecords_ProductPackings_ProductPackingId",
                table: "BatchPackagingRecords");

            migrationBuilder.DropIndex(
                name: "IX_BatchPackagingRecords_ProductPackingId",
                table: "BatchPackagingRecords");

            migrationBuilder.DropColumn(
                name: "ProductPackingId",
                table: "BatchPackagingRecords");
        }
    }
}
