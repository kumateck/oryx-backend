using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionActivityStepToAnalyticalTest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProductionActivityStepId",
                table: "AnalyticalTestRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticalTestRequests_ProductionActivityStepId",
                table: "AnalyticalTestRequests",
                column: "ProductionActivityStepId");

            migrationBuilder.AddForeignKey(
                name: "FK_AnalyticalTestRequests_ProductionActivitySteps_ProductionAc~",
                table: "AnalyticalTestRequests",
                column: "ProductionActivityStepId",
                principalTable: "ProductionActivitySteps",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AnalyticalTestRequests_ProductionActivitySteps_ProductionAc~",
                table: "AnalyticalTestRequests");

            migrationBuilder.DropIndex(
                name: "IX_AnalyticalTestRequests_ProductionActivityStepId",
                table: "AnalyticalTestRequests");

            migrationBuilder.DropColumn(
                name: "ProductionActivityStepId",
                table: "AnalyticalTestRequests");
        }
    }
}
