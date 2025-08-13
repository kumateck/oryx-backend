using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionActivityStepToFormResponse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProductionActivityStepId",
                table: "Responses",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Responses_ProductionActivityStepId",
                table: "Responses",
                column: "ProductionActivityStepId");

            migrationBuilder.AddForeignKey(
                name: "FK_Responses_ProductionActivitySteps_ProductionActivityStepId",
                table: "Responses",
                column: "ProductionActivityStepId",
                principalTable: "ProductionActivitySteps",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Responses_ProductionActivitySteps_ProductionActivityStepId",
                table: "Responses");

            migrationBuilder.DropIndex(
                name: "IX_Responses_ProductionActivityStepId",
                table: "Responses");

            migrationBuilder.DropColumn(
                name: "ProductionActivityStepId",
                table: "Responses");
        }
    }
}
