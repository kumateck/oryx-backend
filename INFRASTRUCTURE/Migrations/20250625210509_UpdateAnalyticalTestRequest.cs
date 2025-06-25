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
            migrationBuilder.DropColumn(
                name: "BatchNumber",
                table: "AnalyticalTestRequests");

            migrationBuilder.DropColumn(
                name: "ProductName",
                table: "AnalyticalTestRequests");

            migrationBuilder.DropColumn(
                name: "ProductSchedule",
                table: "AnalyticalTestRequests");

            migrationBuilder.DropColumn(
                name: "QaManagerSignature",
                table: "AnalyticalTestRequests");

            migrationBuilder.DropColumn(
                name: "QcManagerSignature",
                table: "AnalyticalTestRequests");

            migrationBuilder.AddColumn<Guid>(
                name: "BatchManufacturingRecordId",
                table: "AnalyticalTestRequests",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ProductId",
                table: "AnalyticalTestRequests",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ProductionScheduleId",
                table: "AnalyticalTestRequests",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticalTestRequests_BatchManufacturingRecordId",
                table: "AnalyticalTestRequests",
                column: "BatchManufacturingRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticalTestRequests_ProductId",
                table: "AnalyticalTestRequests",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticalTestRequests_ProductionScheduleId",
                table: "AnalyticalTestRequests",
                column: "ProductionScheduleId");

            migrationBuilder.AddForeignKey(
                name: "FK_AnalyticalTestRequests_BatchManufacturingRecords_BatchManuf~",
                table: "AnalyticalTestRequests",
                column: "BatchManufacturingRecordId",
                principalTable: "BatchManufacturingRecords",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AnalyticalTestRequests_BatchManufacturingRecords_BatchManuf~",
                table: "AnalyticalTestRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_AnalyticalTestRequests_ProductionSchedules_ProductionSchedu~",
                table: "AnalyticalTestRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_AnalyticalTestRequests_Products_ProductId",
                table: "AnalyticalTestRequests");

            migrationBuilder.DropIndex(
                name: "IX_AnalyticalTestRequests_BatchManufacturingRecordId",
                table: "AnalyticalTestRequests");

            migrationBuilder.DropIndex(
                name: "IX_AnalyticalTestRequests_ProductId",
                table: "AnalyticalTestRequests");

            migrationBuilder.DropIndex(
                name: "IX_AnalyticalTestRequests_ProductionScheduleId",
                table: "AnalyticalTestRequests");

            migrationBuilder.DropColumn(
                name: "BatchManufacturingRecordId",
                table: "AnalyticalTestRequests");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "AnalyticalTestRequests");

            migrationBuilder.DropColumn(
                name: "ProductionScheduleId",
                table: "AnalyticalTestRequests");

            migrationBuilder.AddColumn<string>(
                name: "BatchNumber",
                table: "AnalyticalTestRequests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductName",
                table: "AnalyticalTestRequests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductSchedule",
                table: "AnalyticalTestRequests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QaManagerSignature",
                table: "AnalyticalTestRequests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QcManagerSignature",
                table: "AnalyticalTestRequests",
                type: "text",
                nullable: true);
        }
    }
}
