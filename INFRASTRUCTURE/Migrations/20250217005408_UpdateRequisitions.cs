using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRequisitions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Requisitions_ProductionActivitySteps_ActivityStepId",
                table: "Requisitions");

            migrationBuilder.RenameColumn(
                name: "ActivityStepId",
                table: "Requisitions",
                newName: "ProductionScheduleId");

            migrationBuilder.RenameIndex(
                name: "IX_Requisitions_ActivityStepId",
                table: "Requisitions",
                newName: "IX_Requisitions_ProductionScheduleId");

            migrationBuilder.AddColumn<Guid>(
                name: "ProductId",
                table: "Requisitions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProductionActivityStepId",
                table: "Requisitions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Requisitions_ProductId",
                table: "Requisitions",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Requisitions_ProductionActivityStepId",
                table: "Requisitions",
                column: "ProductionActivityStepId");

            migrationBuilder.AddForeignKey(
                name: "FK_Requisitions_ProductionActivitySteps_ProductionActivityStep~",
                table: "Requisitions",
                column: "ProductionActivityStepId",
                principalTable: "ProductionActivitySteps",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Requisitions_ProductionSchedules_ProductionScheduleId",
                table: "Requisitions",
                column: "ProductionScheduleId",
                principalTable: "ProductionSchedules",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Requisitions_Products_ProductId",
                table: "Requisitions",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Requisitions_ProductionActivitySteps_ProductionActivityStep~",
                table: "Requisitions");

            migrationBuilder.DropForeignKey(
                name: "FK_Requisitions_ProductionSchedules_ProductionScheduleId",
                table: "Requisitions");

            migrationBuilder.DropForeignKey(
                name: "FK_Requisitions_Products_ProductId",
                table: "Requisitions");

            migrationBuilder.DropIndex(
                name: "IX_Requisitions_ProductId",
                table: "Requisitions");

            migrationBuilder.DropIndex(
                name: "IX_Requisitions_ProductionActivityStepId",
                table: "Requisitions");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "Requisitions");

            migrationBuilder.DropColumn(
                name: "ProductionActivityStepId",
                table: "Requisitions");

            migrationBuilder.RenameColumn(
                name: "ProductionScheduleId",
                table: "Requisitions",
                newName: "ActivityStepId");

            migrationBuilder.RenameIndex(
                name: "IX_Requisitions_ProductionScheduleId",
                table: "Requisitions",
                newName: "IX_Requisitions_ActivityStepId");

            migrationBuilder.AddForeignKey(
                name: "FK_Requisitions_ProductionActivitySteps_ActivityStepId",
                table: "Requisitions",
                column: "ActivityStepId",
                principalTable: "ProductionActivitySteps",
                principalColumn: "Id");
        }
    }
}
