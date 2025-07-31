using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class RefactorInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Inventories_MaterialBatches_MaterialBatchId",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Inventories_MaterialBatchId",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "MaterialBatchId",
                table: "Items");

            migrationBuilder.RenameColumn(
                name: "HasBatchNumber",
                table: "Items",
                newName: "HasBatch");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "HasBatch",
                table: "Items",
                newName: "HasBatchNumber");

            migrationBuilder.AddColumn<Guid>(
                name: "MaterialBatchId",
                table: "Items",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_MaterialBatchId",
                table: "Items",
                column: "MaterialBatchId");

            migrationBuilder.AddForeignKey(
                name: "FK_Inventories_MaterialBatches_MaterialBatchId",
                table: "Items",
                column: "MaterialBatchId",
                principalTable: "MaterialBatches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
