using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSwapRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "StockRequisitionId",
                table: "SwapRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SwapRequests_StockRequisitionId",
                table: "SwapRequests",
                column: "StockRequisitionId");

            migrationBuilder.AddForeignKey(
                name: "FK_SwapRequests_Requisitions_StockRequisitionId",
                table: "SwapRequests",
                column: "StockRequisitionId",
                principalTable: "Requisitions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SwapRequests_Requisitions_StockRequisitionId",
                table: "SwapRequests");

            migrationBuilder.DropIndex(
                name: "IX_SwapRequests_StockRequisitionId",
                table: "SwapRequests");

            migrationBuilder.DropColumn(
                name: "StockRequisitionId",
                table: "SwapRequests");
        }
    }
}
