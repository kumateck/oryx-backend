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
                table: "MaterialAnalyticalRawData");

            migrationBuilder.DropIndex(
                name: "IX_AnalyticalRawData_MaterialId",
                table: "MaterialAnalyticalRawData");

            migrationBuilder.RenameColumn(
                name: "MaterialId",
                table: "MaterialAnalyticalRawData",
                newName: "StpId");

            migrationBuilder.AddColumn<Guid>(
                name: "MaterialStandardTestProceduresId",
                table: "MaterialAnalyticalRawData",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticalRawData_MaterialStandardTestProceduresId",
                table: "MaterialAnalyticalRawData",
                column: "MaterialStandardTestProceduresId");

            migrationBuilder.AddForeignKey(
                name: "FK_AnalyticalRawData_MaterialStandardTestProcedures_MaterialSt~",
                table: "MaterialAnalyticalRawData",
                column: "MaterialStandardTestProceduresId",
                principalTable: "MaterialStandardTestProcedures",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AnalyticalRawData_MaterialStandardTestProcedures_MaterialSt~",
                table: "MaterialAnalyticalRawData");

            migrationBuilder.DropIndex(
                name: "IX_AnalyticalRawData_MaterialStandardTestProceduresId",
                table: "MaterialAnalyticalRawData");

            migrationBuilder.DropColumn(
                name: "MaterialStandardTestProceduresId",
                table: "MaterialAnalyticalRawData");

            migrationBuilder.RenameColumn(
                name: "StpId",
                table: "MaterialAnalyticalRawData",
                newName: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticalRawData_MaterialId",
                table: "MaterialAnalyticalRawData",
                column: "MaterialId");

            migrationBuilder.AddForeignKey(
                name: "FK_AnalyticalRawData_Materials_MaterialId",
                table: "MaterialAnalyticalRawData",
                column: "MaterialId",
                principalTable: "Materials",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
