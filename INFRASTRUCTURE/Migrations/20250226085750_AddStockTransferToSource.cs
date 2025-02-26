using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddStockTransferToSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockTransferSources_StockTransfers_StockTransferId",
                table: "StockTransferSources");

            migrationBuilder.AlterColumn<Guid>(
                name: "StockTransferId",
                table: "StockTransferSources",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_StockTransferSources_StockTransfers_StockTransferId",
                table: "StockTransferSources",
                column: "StockTransferId",
                principalTable: "StockTransfers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockTransferSources_StockTransfers_StockTransferId",
                table: "StockTransferSources");

            migrationBuilder.AlterColumn<Guid>(
                name: "StockTransferId",
                table: "StockTransferSources",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_StockTransferSources_StockTransfers_StockTransferId",
                table: "StockTransferSources",
                column: "StockTransferId",
                principalTable: "StockTransfers",
                principalColumn: "Id");
        }
    }
}
