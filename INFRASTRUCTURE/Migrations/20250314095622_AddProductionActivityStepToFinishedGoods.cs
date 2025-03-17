using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionActivityStepToFinishedGoods : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProductionActivityStepId",
                table: "FinishedGoodsTransferNotes",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinishedGoodsTransferNotes_ProductionActivityStepId",
                table: "FinishedGoodsTransferNotes",
                column: "ProductionActivityStepId");

            migrationBuilder.AddForeignKey(
                name: "FK_FinishedGoodsTransferNotes_ProductionActivitySteps_Producti~",
                table: "FinishedGoodsTransferNotes",
                column: "ProductionActivityStepId",
                principalTable: "ProductionActivitySteps",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FinishedGoodsTransferNotes_ProductionActivitySteps_Producti~",
                table: "FinishedGoodsTransferNotes");

            migrationBuilder.DropIndex(
                name: "IX_FinishedGoodsTransferNotes_ProductionActivityStepId",
                table: "FinishedGoodsTransferNotes");

            migrationBuilder.DropColumn(
                name: "ProductionActivityStepId",
                table: "FinishedGoodsTransferNotes");
        }
    }
}
