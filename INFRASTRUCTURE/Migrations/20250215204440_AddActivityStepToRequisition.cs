using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddActivityStepToRequisition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ActivityStepId",
                table: "Requisitions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Requisitions_ActivityStepId",
                table: "Requisitions",
                column: "ActivityStepId");

            migrationBuilder.AddForeignKey(
                name: "FK_Requisitions_ProductionActivitySteps_ActivityStepId",
                table: "Requisitions",
                column: "ActivityStepId",
                principalTable: "ProductionActivitySteps",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Requisitions_ProductionActivitySteps_ActivityStepId",
                table: "Requisitions");

            migrationBuilder.DropIndex(
                name: "IX_Requisitions_ActivityStepId",
                table: "Requisitions");

            migrationBuilder.DropColumn(
                name: "ActivityStepId",
                table: "Requisitions");
        }
    }
}
