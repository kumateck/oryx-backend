using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialBatchToPartialReturn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "MaterialBatchId",
                table: "MaterialReturnNotePartialReturns",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaterialReturnNotePartialReturns_MaterialBatchId",
                table: "MaterialReturnNotePartialReturns",
                column: "MaterialBatchId");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialReturnNotePartialReturns_MaterialBatches_MaterialBa~",
                table: "MaterialReturnNotePartialReturns",
                column: "MaterialBatchId",
                principalTable: "MaterialBatches",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialReturnNotePartialReturns_MaterialBatches_MaterialBa~",
                table: "MaterialReturnNotePartialReturns");

            migrationBuilder.DropIndex(
                name: "IX_MaterialReturnNotePartialReturns_MaterialBatchId",
                table: "MaterialReturnNotePartialReturns");

            migrationBuilder.DropColumn(
                name: "MaterialBatchId",
                table: "MaterialReturnNotePartialReturns");
        }
    }
}
