using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class Updated_StockTransfer_UoM : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockTransferSources_UnitOfMeasures_UoMId",
                table: "StockTransferSources");

            migrationBuilder.DropIndex(
                name: "IX_StockTransferSources_UoMId",
                table: "StockTransferSources");

            migrationBuilder.DropColumn(
                name: "UoMId",
                table: "StockTransferSources");

            migrationBuilder.AddColumn<Guid>(
                name: "UoMId",
                table: "StockTransfers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_UoMId",
                table: "StockTransfers",
                column: "UoMId");

            migrationBuilder.AddForeignKey(
                name: "FK_StockTransfers_UnitOfMeasures_UoMId",
                table: "StockTransfers",
                column: "UoMId",
                principalTable: "UnitOfMeasures",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockTransfers_UnitOfMeasures_UoMId",
                table: "StockTransfers");

            migrationBuilder.DropIndex(
                name: "IX_StockTransfers_UoMId",
                table: "StockTransfers");

            migrationBuilder.DropColumn(
                name: "UoMId",
                table: "StockTransfers");

            migrationBuilder.AddColumn<Guid>(
                name: "UoMId",
                table: "StockTransferSources",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferSources_UoMId",
                table: "StockTransferSources",
                column: "UoMId");

            migrationBuilder.AddForeignKey(
                name: "FK_StockTransferSources_UnitOfMeasures_UoMId",
                table: "StockTransferSources",
                column: "UoMId",
                principalTable: "UnitOfMeasures",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
