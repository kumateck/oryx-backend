using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddTestForArd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "MaterialBatchId",
                table: "MaterialAnalyticalRawData",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaterialAnalyticalRawData_MaterialBatchId",
                table: "MaterialAnalyticalRawData",
                column: "MaterialBatchId");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialAnalyticalRawData_MaterialBatches_MaterialBatchId",
                table: "MaterialAnalyticalRawData",
                column: "MaterialBatchId",
                principalTable: "MaterialBatches",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialAnalyticalRawData_MaterialBatches_MaterialBatchId",
                table: "MaterialAnalyticalRawData");

            migrationBuilder.DropIndex(
                name: "IX_MaterialAnalyticalRawData_MaterialBatchId",
                table: "MaterialAnalyticalRawData");

            migrationBuilder.DropColumn(
                name: "MaterialBatchId",
                table: "MaterialAnalyticalRawData");
        }
    }
}
