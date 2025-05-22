using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddCodeToOvertimeRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AnalyticalRawData_MaterialStandardTestProcedures_MaterialSt~",
                table: "AnalyticalRawData");

            migrationBuilder.DropIndex(
                name: "IX_AnalyticalRawData_MaterialStandardTestProcedureId",
                table: "AnalyticalRawData");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "OvertimeRequests");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "OvertimeRequests");

            migrationBuilder.DropColumn(
                name: "MaterialStandardTestProcedureId",
                table: "AnalyticalRawData");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "OvertimeRequests",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticalRawData_StpId",
                table: "AnalyticalRawData",
                column: "StpId");

            migrationBuilder.AddForeignKey(
                name: "FK_AnalyticalRawData_MaterialStandardTestProcedures_StpId",
                table: "AnalyticalRawData",
                column: "StpId",
                principalTable: "MaterialStandardTestProcedures",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AnalyticalRawData_MaterialStandardTestProcedures_StpId",
                table: "AnalyticalRawData");

            migrationBuilder.DropIndex(
                name: "IX_AnalyticalRawData_StpId",
                table: "AnalyticalRawData");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "OvertimeRequests");

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "OvertimeRequests",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "OvertimeRequests",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "MaterialStandardTestProcedureId",
                table: "AnalyticalRawData",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticalRawData_MaterialStandardTestProcedureId",
                table: "AnalyticalRawData",
                column: "MaterialStandardTestProcedureId");

            migrationBuilder.AddForeignKey(
                name: "FK_AnalyticalRawData_MaterialStandardTestProcedures_MaterialSt~",
                table: "AnalyticalRawData",
                column: "MaterialStandardTestProcedureId",
                principalTable: "MaterialStandardTestProcedures",
                principalColumn: "Id");
        }
    }
}
