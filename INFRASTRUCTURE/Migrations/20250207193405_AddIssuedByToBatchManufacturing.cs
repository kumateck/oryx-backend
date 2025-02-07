using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddIssuedByToBatchManufacturing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "IssuedById",
                table: "BatchPackagingRecords",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "IssuedById",
                table: "BatchManufacturingRecords",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BatchPackagingRecords_IssuedById",
                table: "BatchPackagingRecords",
                column: "IssuedById");

            migrationBuilder.CreateIndex(
                name: "IX_BatchManufacturingRecords_IssuedById",
                table: "BatchManufacturingRecords",
                column: "IssuedById");

            migrationBuilder.AddForeignKey(
                name: "FK_BatchManufacturingRecords_users_IssuedById",
                table: "BatchManufacturingRecords",
                column: "IssuedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BatchPackagingRecords_users_IssuedById",
                table: "BatchPackagingRecords",
                column: "IssuedById",
                principalTable: "users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BatchManufacturingRecords_users_IssuedById",
                table: "BatchManufacturingRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_BatchPackagingRecords_users_IssuedById",
                table: "BatchPackagingRecords");

            migrationBuilder.DropIndex(
                name: "IX_BatchPackagingRecords_IssuedById",
                table: "BatchPackagingRecords");

            migrationBuilder.DropIndex(
                name: "IX_BatchManufacturingRecords_IssuedById",
                table: "BatchManufacturingRecords");

            migrationBuilder.DropColumn(
                name: "IssuedById",
                table: "BatchPackagingRecords");

            migrationBuilder.DropColumn(
                name: "IssuedById",
                table: "BatchManufacturingRecords");
        }
    }
}
