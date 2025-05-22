using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddMStpAndRemoveMaterialARD : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AnalyticalRawData_Materials_MaterialId",
                table: "AnalyticalRawData");

            migrationBuilder.DropIndex(
                name: "IX_AnalyticalRawData_MaterialId",
                table: "AnalyticalRawData");

            migrationBuilder.RenameColumn(
                name: "MaterialId",
                table: "AnalyticalRawData",
                newName: "StpId");

            migrationBuilder.AddColumn<Guid>(
                name: "MaterialStandardTestProceduresId",
                table: "AnalyticalRawData",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticalRawData_MaterialStandardTestProceduresId",
                table: "AnalyticalRawData",
                column: "MaterialStandardTestProceduresId");

            migrationBuilder.AddForeignKey(
                name: "FK_AnalyticalRawData_MaterialStandardTestProcedures_MaterialSt~",
                table: "AnalyticalRawData",
                column: "MaterialStandardTestProceduresId",
                principalTable: "MaterialStandardTestProcedures",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AnalyticalRawData_MaterialStandardTestProcedures_MaterialSt~",
                table: "AnalyticalRawData");

            migrationBuilder.DropIndex(
                name: "IX_AnalyticalRawData_MaterialStandardTestProceduresId",
                table: "AnalyticalRawData");

            migrationBuilder.DropColumn(
                name: "MaterialStandardTestProceduresId",
                table: "AnalyticalRawData");

            migrationBuilder.RenameColumn(
                name: "StpId",
                table: "AnalyticalRawData",
                newName: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticalRawData_MaterialId",
                table: "AnalyticalRawData",
                column: "MaterialId");

            migrationBuilder.AddForeignKey(
                name: "FK_AnalyticalRawData_Materials_MaterialId",
                table: "AnalyticalRawData",
                column: "MaterialId",
                principalTable: "Materials",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
