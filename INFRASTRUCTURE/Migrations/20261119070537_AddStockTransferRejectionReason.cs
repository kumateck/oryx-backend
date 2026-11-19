using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddStockTransferRejectionReason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RejectedAt",
                table: "StockTransferSources",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RejectedById",
                table: "StockTransferSources",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "StockTransferSources",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferSources_RejectedById",
                table: "StockTransferSources",
                column: "RejectedById");

            migrationBuilder.AddForeignKey(
                name: "FK_StockTransferSources_users_RejectedById",
                table: "StockTransferSources",
                column: "RejectedById",
                principalTable: "users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockTransferSources_users_RejectedById",
                table: "StockTransferSources");

            migrationBuilder.DropIndex(
                name: "IX_StockTransferSources_RejectedById",
                table: "StockTransferSources");

            migrationBuilder.DropColumn(
                name: "RejectedAt",
                table: "StockTransferSources");

            migrationBuilder.DropColumn(
                name: "RejectedById",
                table: "StockTransferSources");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "StockTransferSources");
        }
    }
}
