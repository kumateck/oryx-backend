using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class RenameHoldingMaterialContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GHoldingMaterialTransfers_users_CreatedById",
                table: "GHoldingMaterialTransfers");

            migrationBuilder.DropForeignKey(
                name: "FK_GHoldingMaterialTransfers_users_LastDeletedById",
                table: "GHoldingMaterialTransfers");

            migrationBuilder.DropForeignKey(
                name: "FK_GHoldingMaterialTransfers_users_LastUpdatedById",
                table: "GHoldingMaterialTransfers");

            migrationBuilder.DropForeignKey(
                name: "FK_HoldingMaterialTransferBatches_GHoldingMaterialTransfers_Ho~",
                table: "HoldingMaterialTransferBatches");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GHoldingMaterialTransfers",
                table: "GHoldingMaterialTransfers");

            migrationBuilder.RenameTable(
                name: "GHoldingMaterialTransfers",
                newName: "HoldingMaterialTransfers");

            migrationBuilder.RenameIndex(
                name: "IX_GHoldingMaterialTransfers_LastUpdatedById",
                table: "HoldingMaterialTransfers",
                newName: "IX_HoldingMaterialTransfers_LastUpdatedById");

            migrationBuilder.RenameIndex(
                name: "IX_GHoldingMaterialTransfers_LastDeletedById",
                table: "HoldingMaterialTransfers",
                newName: "IX_HoldingMaterialTransfers_LastDeletedById");

            migrationBuilder.RenameIndex(
                name: "IX_GHoldingMaterialTransfers_CreatedById",
                table: "HoldingMaterialTransfers",
                newName: "IX_HoldingMaterialTransfers_CreatedById");

            migrationBuilder.AddColumn<Guid>(
                name: "StockTransferId",
                table: "HoldingMaterialTransfers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_HoldingMaterialTransfers",
                table: "HoldingMaterialTransfers",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_HoldingMaterialTransfers_StockTransferId",
                table: "HoldingMaterialTransfers",
                column: "StockTransferId");

            migrationBuilder.AddForeignKey(
                name: "FK_HoldingMaterialTransferBatches_HoldingMaterialTransfers_Hol~",
                table: "HoldingMaterialTransferBatches",
                column: "HoldingMaterialTransferId",
                principalTable: "HoldingMaterialTransfers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_HoldingMaterialTransfers_StockTransfers_StockTransferId",
                table: "HoldingMaterialTransfers",
                column: "StockTransferId",
                principalTable: "StockTransfers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_HoldingMaterialTransfers_users_CreatedById",
                table: "HoldingMaterialTransfers",
                column: "CreatedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_HoldingMaterialTransfers_users_LastDeletedById",
                table: "HoldingMaterialTransfers",
                column: "LastDeletedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_HoldingMaterialTransfers_users_LastUpdatedById",
                table: "HoldingMaterialTransfers",
                column: "LastUpdatedById",
                principalTable: "users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HoldingMaterialTransferBatches_HoldingMaterialTransfers_Hol~",
                table: "HoldingMaterialTransferBatches");

            migrationBuilder.DropForeignKey(
                name: "FK_HoldingMaterialTransfers_StockTransfers_StockTransferId",
                table: "HoldingMaterialTransfers");

            migrationBuilder.DropForeignKey(
                name: "FK_HoldingMaterialTransfers_users_CreatedById",
                table: "HoldingMaterialTransfers");

            migrationBuilder.DropForeignKey(
                name: "FK_HoldingMaterialTransfers_users_LastDeletedById",
                table: "HoldingMaterialTransfers");

            migrationBuilder.DropForeignKey(
                name: "FK_HoldingMaterialTransfers_users_LastUpdatedById",
                table: "HoldingMaterialTransfers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_HoldingMaterialTransfers",
                table: "HoldingMaterialTransfers");

            migrationBuilder.DropIndex(
                name: "IX_HoldingMaterialTransfers_StockTransferId",
                table: "HoldingMaterialTransfers");

            migrationBuilder.DropColumn(
                name: "StockTransferId",
                table: "HoldingMaterialTransfers");

            migrationBuilder.RenameTable(
                name: "HoldingMaterialTransfers",
                newName: "GHoldingMaterialTransfers");

            migrationBuilder.RenameIndex(
                name: "IX_HoldingMaterialTransfers_LastUpdatedById",
                table: "GHoldingMaterialTransfers",
                newName: "IX_GHoldingMaterialTransfers_LastUpdatedById");

            migrationBuilder.RenameIndex(
                name: "IX_HoldingMaterialTransfers_LastDeletedById",
                table: "GHoldingMaterialTransfers",
                newName: "IX_GHoldingMaterialTransfers_LastDeletedById");

            migrationBuilder.RenameIndex(
                name: "IX_HoldingMaterialTransfers_CreatedById",
                table: "GHoldingMaterialTransfers",
                newName: "IX_GHoldingMaterialTransfers_CreatedById");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GHoldingMaterialTransfers",
                table: "GHoldingMaterialTransfers",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_GHoldingMaterialTransfers_users_CreatedById",
                table: "GHoldingMaterialTransfers",
                column: "CreatedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_GHoldingMaterialTransfers_users_LastDeletedById",
                table: "GHoldingMaterialTransfers",
                column: "LastDeletedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_GHoldingMaterialTransfers_users_LastUpdatedById",
                table: "GHoldingMaterialTransfers",
                column: "LastUpdatedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_HoldingMaterialTransferBatches_GHoldingMaterialTransfers_Ho~",
                table: "HoldingMaterialTransferBatches",
                column: "HoldingMaterialTransferId",
                principalTable: "GHoldingMaterialTransfers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
