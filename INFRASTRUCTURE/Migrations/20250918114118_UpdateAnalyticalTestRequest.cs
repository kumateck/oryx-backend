using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAnalyticalTestRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AnalyticalTestRequests_ProductionActivitySteps_ProductionAc~",
                table: "AnalyticalTestRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_AnalyticalTestRequests_ProductionSchedules_ProductionSchedu~",
                table: "AnalyticalTestRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_AnalyticalTestRequests_Products_ProductId",
                table: "AnalyticalTestRequests");

            migrationBuilder.DropIndex(
                name: "IX_AnalyticalTestRequests_ProductId",
                table: "AnalyticalTestRequests");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "AnalyticalTestRequests");

            migrationBuilder.RenameColumn(
                name: "ProductionScheduleId",
                table: "AnalyticalTestRequests",
                newName: "ProductionScheduleProductId");

            migrationBuilder.RenameIndex(
                name: "IX_AnalyticalTestRequests_ProductionScheduleId",
                table: "AnalyticalTestRequests",
                newName: "IX_AnalyticalTestRequests_ProductionScheduleProductId");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProductionActivityStepId",
                table: "AnalyticalTestRequests",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AnalyticalTestRequests_ProductionActivitySteps_ProductionAc~",
                table: "AnalyticalTestRequests",
                column: "ProductionActivityStepId",
                principalTable: "ProductionActivitySteps",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AnalyticalTestRequests_ProductionScheduleProducts_Productio~",
                table: "AnalyticalTestRequests",
                column: "ProductionScheduleProductId",
                principalTable: "ProductionScheduleProducts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AnalyticalTestRequests_ProductionActivitySteps_ProductionAc~",
                table: "AnalyticalTestRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_AnalyticalTestRequests_ProductionScheduleProducts_Productio~",
                table: "AnalyticalTestRequests");

            migrationBuilder.RenameColumn(
                name: "ProductionScheduleProductId",
                table: "AnalyticalTestRequests",
                newName: "ProductionScheduleId");

            migrationBuilder.RenameIndex(
                name: "IX_AnalyticalTestRequests_ProductionScheduleProductId",
                table: "AnalyticalTestRequests",
                newName: "IX_AnalyticalTestRequests_ProductionScheduleId");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProductionActivityStepId",
                table: "AnalyticalTestRequests",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "ProductId",
                table: "AnalyticalTestRequests",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticalTestRequests_ProductId",
                table: "AnalyticalTestRequests",
                column: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_AnalyticalTestRequests_ProductionActivitySteps_ProductionAc~",
                table: "AnalyticalTestRequests",
                column: "ProductionActivityStepId",
                principalTable: "ProductionActivitySteps",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AnalyticalTestRequests_ProductionSchedules_ProductionSchedu~",
                table: "AnalyticalTestRequests",
                column: "ProductionScheduleId",
                principalTable: "ProductionSchedules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AnalyticalTestRequests_Products_ProductId",
                table: "AnalyticalTestRequests",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
