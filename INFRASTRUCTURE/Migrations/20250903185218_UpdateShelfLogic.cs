using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class UpdateShelfLogic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SourceWarehouseLocationShelfId",
                table: "MaterialReturnNotePartialReturns",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceWarehouseLocationShelfId",
                table: "MaterialReturnNoteFullReturns",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WarehouseLocationShelfId",
                table: "MaterialBatchReservedQuantities",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaterialReturnNotePartialReturns_SourceWarehouseLocationShe~",
                table: "MaterialReturnNotePartialReturns",
                column: "SourceWarehouseLocationShelfId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialReturnNoteFullReturns_SourceWarehouseLocationShelfId",
                table: "MaterialReturnNoteFullReturns",
                column: "SourceWarehouseLocationShelfId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialBatchReservedQuantities_WarehouseLocationShelfId",
                table: "MaterialBatchReservedQuantities",
                column: "WarehouseLocationShelfId");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialBatchReservedQuantities_WarehouseLocationShelves_Wa~",
                table: "MaterialBatchReservedQuantities",
                column: "WarehouseLocationShelfId",
                principalTable: "WarehouseLocationShelves",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialReturnNoteFullReturns_WarehouseLocationShelves_Sour~",
                table: "MaterialReturnNoteFullReturns",
                column: "SourceWarehouseLocationShelfId",
                principalTable: "WarehouseLocationShelves",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialReturnNotePartialReturns_WarehouseLocationShelves_S~",
                table: "MaterialReturnNotePartialReturns",
                column: "SourceWarehouseLocationShelfId",
                principalTable: "WarehouseLocationShelves",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialBatchReservedQuantities_WarehouseLocationShelves_Wa~",
                table: "MaterialBatchReservedQuantities");

            migrationBuilder.DropForeignKey(
                name: "FK_MaterialReturnNoteFullReturns_WarehouseLocationShelves_Sour~",
                table: "MaterialReturnNoteFullReturns");

            migrationBuilder.DropForeignKey(
                name: "FK_MaterialReturnNotePartialReturns_WarehouseLocationShelves_S~",
                table: "MaterialReturnNotePartialReturns");

            migrationBuilder.DropIndex(
                name: "IX_MaterialReturnNotePartialReturns_SourceWarehouseLocationShe~",
                table: "MaterialReturnNotePartialReturns");

            migrationBuilder.DropIndex(
                name: "IX_MaterialReturnNoteFullReturns_SourceWarehouseLocationShelfId",
                table: "MaterialReturnNoteFullReturns");

            migrationBuilder.DropIndex(
                name: "IX_MaterialBatchReservedQuantities_WarehouseLocationShelfId",
                table: "MaterialBatchReservedQuantities");

            migrationBuilder.DropColumn(
                name: "SourceWarehouseLocationShelfId",
                table: "MaterialReturnNotePartialReturns");

            migrationBuilder.DropColumn(
                name: "SourceWarehouseLocationShelfId",
                table: "MaterialReturnNoteFullReturns");

            migrationBuilder.DropColumn(
                name: "WarehouseLocationShelfId",
                table: "MaterialBatchReservedQuantities");
        }
    }
}
