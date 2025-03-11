using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class UpdateStockTransfer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ApprovedById",
                table: "StockTransfers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "IssuedAt",
                table: "StockTransfers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "IssuedById",
                table: "StockTransfers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "StockTransfers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_ApprovedById",
                table: "StockTransfers",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_IssuedById",
                table: "StockTransfers",
                column: "IssuedById");

            migrationBuilder.AddForeignKey(
                name: "FK_StockTransfers_users_ApprovedById",
                table: "StockTransfers",
                column: "ApprovedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StockTransfers_users_IssuedById",
                table: "StockTransfers",
                column: "IssuedById",
                principalTable: "users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockTransfers_users_ApprovedById",
                table: "StockTransfers");

            migrationBuilder.DropForeignKey(
                name: "FK_StockTransfers_users_IssuedById",
                table: "StockTransfers");

            migrationBuilder.DropIndex(
                name: "IX_StockTransfers_ApprovedById",
                table: "StockTransfers");

            migrationBuilder.DropIndex(
                name: "IX_StockTransfers_IssuedById",
                table: "StockTransfers");

            migrationBuilder.DropColumn(
                name: "ApprovedById",
                table: "StockTransfers");

            migrationBuilder.DropColumn(
                name: "IssuedAt",
                table: "StockTransfers");

            migrationBuilder.DropColumn(
                name: "IssuedById",
                table: "StockTransfers");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "StockTransfers");
        }
    }
}
