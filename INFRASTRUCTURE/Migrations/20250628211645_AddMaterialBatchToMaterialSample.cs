using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialBatchToMaterialSample : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "MaterialBatchId",
                table: "MaterialSamplings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_MaterialSamplings_MaterialBatchId",
                table: "MaterialSamplings",
                column: "MaterialBatchId");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialSamplings_MaterialBatches_MaterialBatchId",
                table: "MaterialSamplings",
                column: "MaterialBatchId",
                principalTable: "MaterialBatches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialSamplings_MaterialBatches_MaterialBatchId",
                table: "MaterialSamplings");

            migrationBuilder.DropIndex(
                name: "IX_MaterialSamplings_MaterialBatchId",
                table: "MaterialSamplings");

            migrationBuilder.DropColumn(
                name: "MaterialBatchId",
                table: "MaterialSamplings");
        }
    }
}
