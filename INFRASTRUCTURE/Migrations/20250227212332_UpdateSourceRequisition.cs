using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSourceRequisition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SourceRequisitions_Requisitions_RequisitionId",
                table: "SourceRequisitions");

            migrationBuilder.DropIndex(
                name: "IX_SourceRequisitions_RequisitionId",
                table: "SourceRequisitions");

            migrationBuilder.DropColumn(
                name: "RequisitionId",
                table: "SourceRequisitions");

            migrationBuilder.DropColumn(
                name: "RequisitionIds",
                table: "SourceRequisitions");

            migrationBuilder.AddColumn<Guid>(
                name: "ProductId",
                table: "StockTransfers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProductionActivityStepId",
                table: "StockTransfers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProductionScheduleId",
                table: "StockTransfers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RequisitionId",
                table: "SourceRequisitionItems",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_ProductId",
                table: "StockTransfers",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_ProductionActivityStepId",
                table: "StockTransfers",
                column: "ProductionActivityStepId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_ProductionScheduleId",
                table: "StockTransfers",
                column: "ProductionScheduleId");

            migrationBuilder.AddForeignKey(
                name: "FK_StockTransfers_ProductionActivitySteps_ProductionActivitySt~",
                table: "StockTransfers",
                column: "ProductionActivityStepId",
                principalTable: "ProductionActivitySteps",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StockTransfers_ProductionSchedules_ProductionScheduleId",
                table: "StockTransfers",
                column: "ProductionScheduleId",
                principalTable: "ProductionSchedules",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StockTransfers_Products_ProductId",
                table: "StockTransfers",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockTransfers_ProductionActivitySteps_ProductionActivitySt~",
                table: "StockTransfers");

            migrationBuilder.DropForeignKey(
                name: "FK_StockTransfers_ProductionSchedules_ProductionScheduleId",
                table: "StockTransfers");

            migrationBuilder.DropForeignKey(
                name: "FK_StockTransfers_Products_ProductId",
                table: "StockTransfers");

            migrationBuilder.DropIndex(
                name: "IX_StockTransfers_ProductId",
                table: "StockTransfers");

            migrationBuilder.DropIndex(
                name: "IX_StockTransfers_ProductionActivityStepId",
                table: "StockTransfers");

            migrationBuilder.DropIndex(
                name: "IX_StockTransfers_ProductionScheduleId",
                table: "StockTransfers");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "StockTransfers");

            migrationBuilder.DropColumn(
                name: "ProductionActivityStepId",
                table: "StockTransfers");

            migrationBuilder.DropColumn(
                name: "ProductionScheduleId",
                table: "StockTransfers");

            migrationBuilder.DropColumn(
                name: "RequisitionId",
                table: "SourceRequisitionItems");

            migrationBuilder.AddColumn<Guid>(
                name: "RequisitionId",
                table: "SourceRequisitions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<List<Guid>>(
                name: "RequisitionIds",
                table: "SourceRequisitions",
                type: "uuid[]",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SourceRequisitions_RequisitionId",
                table: "SourceRequisitions",
                column: "RequisitionId");

            migrationBuilder.AddForeignKey(
                name: "FK_SourceRequisitions_Requisitions_RequisitionId",
                table: "SourceRequisitions",
                column: "RequisitionId",
                principalTable: "Requisitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
