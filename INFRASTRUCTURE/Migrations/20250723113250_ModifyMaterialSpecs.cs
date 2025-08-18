using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class ModifyMaterialSpecs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TestName",
                table: "ProductSpecifications_TestSpecifications");

            migrationBuilder.DropColumn(
                name: "TestName",
                table: "MaterialSpecifications_TestSpecifications");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "ProductSpecifications_TestSpecifications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "MaterialSpecifications_TestSpecifications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FormId",
                table: "MaterialSpecifications",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "MaterialAnalyticalRawDataId",
                table: "MaterialSpecifications",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_MaterialSpecifications_FormId",
                table: "MaterialSpecifications",
                column: "FormId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialSpecifications_MaterialAnalyticalRawDataId",
                table: "MaterialSpecifications",
                column: "MaterialAnalyticalRawDataId");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialSpecifications_Forms_FormId",
                table: "MaterialSpecifications",
                column: "FormId",
                principalTable: "Forms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialSpecifications_MaterialAnalyticalRawData_MaterialAn~",
                table: "MaterialSpecifications",
                column: "MaterialAnalyticalRawDataId",
                principalTable: "MaterialAnalyticalRawData",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialSpecifications_Forms_FormId",
                table: "MaterialSpecifications");

            migrationBuilder.DropForeignKey(
                name: "FK_MaterialSpecifications_MaterialAnalyticalRawData_MaterialAn~",
                table: "MaterialSpecifications");

            migrationBuilder.DropIndex(
                name: "IX_MaterialSpecifications_FormId",
                table: "MaterialSpecifications");

            migrationBuilder.DropIndex(
                name: "IX_MaterialSpecifications_MaterialAnalyticalRawDataId",
                table: "MaterialSpecifications");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "ProductSpecifications_TestSpecifications");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "MaterialSpecifications_TestSpecifications");

            migrationBuilder.DropColumn(
                name: "FormId",
                table: "MaterialSpecifications");

            migrationBuilder.DropColumn(
                name: "MaterialAnalyticalRawDataId",
                table: "MaterialSpecifications");

            migrationBuilder.AddColumn<int>(
                name: "TestName",
                table: "ProductSpecifications_TestSpecifications",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TestName",
                table: "MaterialSpecifications_TestSpecifications",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
