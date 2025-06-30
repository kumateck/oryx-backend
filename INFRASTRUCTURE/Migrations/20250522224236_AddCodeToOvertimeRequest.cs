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
                table: "MaterialAnalyticalRawData");

            migrationBuilder.DropIndex(
                name: "IX_AnalyticalRawData_MaterialStandardTestProcedureId",
                table: "MaterialAnalyticalRawData");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "OvertimeRequests");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "OvertimeRequests");

            migrationBuilder.DropColumn(
                name: "MaterialStandardTestProcedureId",
                table: "MaterialAnalyticalRawData");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "OvertimeRequests",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticalRawData_StpId",
                table: "MaterialAnalyticalRawData",
                column: "StpId");

            migrationBuilder.AddForeignKey(
                name: "FK_AnalyticalRawData_MaterialStandardTestProcedures_StpId",
                table: "MaterialAnalyticalRawData",
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
                table: "MaterialAnalyticalRawData");

            migrationBuilder.DropIndex(
                name: "IX_AnalyticalRawData_StpId",
                table: "MaterialAnalyticalRawData");

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
                table: "MaterialAnalyticalRawData",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticalRawData_MaterialStandardTestProcedureId",
                table: "MaterialAnalyticalRawData",
                column: "MaterialStandardTestProcedureId");

            migrationBuilder.AddForeignKey(
                name: "FK_AnalyticalRawData_MaterialStandardTestProcedures_MaterialSt~",
                table: "MaterialAnalyticalRawData",
                column: "MaterialStandardTestProcedureId",
                principalTable: "MaterialStandardTestProcedures",
                principalColumn: "Id");
        }
    }
}
