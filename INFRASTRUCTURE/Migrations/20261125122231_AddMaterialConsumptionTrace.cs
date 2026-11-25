using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialConsumptionTrace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "MaterialBatchReservedQuantityId",
                table: "MaterialBatchEvents",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProductionScheduleProductId",
                table: "MaterialBatchEvents",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaterialBatchEvents_MaterialBatchReservedQuantityId",
                table: "MaterialBatchEvents",
                column: "MaterialBatchReservedQuantityId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialBatchEvents_ProductionScheduleProductId",
                table: "MaterialBatchEvents",
                column: "ProductionScheduleProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialBatchEvents_MaterialBatchReservedQuantities_Materia~",
                table: "MaterialBatchEvents",
                column: "MaterialBatchReservedQuantityId",
                principalTable: "MaterialBatchReservedQuantities",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialBatchEvents_ProductionScheduleProducts_ProductionSc~",
                table: "MaterialBatchEvents",
                column: "ProductionScheduleProductId",
                principalTable: "ProductionScheduleProducts",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialBatchEvents_MaterialBatchReservedQuantities_Materia~",
                table: "MaterialBatchEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_MaterialBatchEvents_ProductionScheduleProducts_ProductionSc~",
                table: "MaterialBatchEvents");

            migrationBuilder.DropIndex(
                name: "IX_MaterialBatchEvents_MaterialBatchReservedQuantityId",
                table: "MaterialBatchEvents");

            migrationBuilder.DropIndex(
                name: "IX_MaterialBatchEvents_ProductionScheduleProductId",
                table: "MaterialBatchEvents");

            migrationBuilder.DropColumn(
                name: "MaterialBatchReservedQuantityId",
                table: "MaterialBatchEvents");

            migrationBuilder.DropColumn(
                name: "ProductionScheduleProductId",
                table: "MaterialBatchEvents");
        }
    }
}
