using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddSwapRequestProductionBatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProductionScheduleProductId",
                table: "SwapRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SwapRequests_ProductionScheduleProductId",
                table: "SwapRequests",
                column: "ProductionScheduleProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_SwapRequests_ProductionScheduleProducts_ProductionScheduleP~",
                table: "SwapRequests",
                column: "ProductionScheduleProductId",
                principalTable: "ProductionScheduleProducts",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SwapRequests_ProductionScheduleProducts_ProductionScheduleP~",
                table: "SwapRequests");

            migrationBuilder.DropIndex(
                name: "IX_SwapRequests_ProductionScheduleProductId",
                table: "SwapRequests");

            migrationBuilder.DropColumn(
                name: "ProductionScheduleProductId",
                table: "SwapRequests");
        }
    }
}
