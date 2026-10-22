using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class TrackStockIssueRequisitionLines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ItemStockRequisitionItemId",
                table: "IssueItemStockRequisitions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_IssueItemStockRequisitions_ItemStockRequisitionItemId",
                table: "IssueItemStockRequisitions",
                column: "ItemStockRequisitionItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_IssueItemStockRequisitions_ItemStockRequisitionItems_ItemSt~",
                table: "IssueItemStockRequisitions",
                column: "ItemStockRequisitionItemId",
                principalTable: "ItemStockRequisitionItems",
                principalColumn: "Id");

            // Earlier issues were recorded against the requisition only. They can be attributed safely when the
            // requisition has a single line; the rest stay unattributed and such requisitions are refused further issues.
            migrationBuilder.Sql("""
                UPDATE "IssueItemStockRequisitions" AS issue
                SET "ItemStockRequisitionItemId" = line."Id"
                FROM "ItemStockRequisitionItems" AS line
                WHERE issue."ItemStockRequisitionItemId" IS NULL
                  AND line."ItemStockRequisitionId" = issue."ItemStockRequisitionId"
                  AND (SELECT COUNT(*) FROM "ItemStockRequisitionItems" AS other
                       WHERE other."ItemStockRequisitionId" = issue."ItemStockRequisitionId") = 1;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IssueItemStockRequisitions_ItemStockRequisitionItems_ItemSt~",
                table: "IssueItemStockRequisitions");

            migrationBuilder.DropIndex(
                name: "IX_IssueItemStockRequisitions_ItemStockRequisitionItemId",
                table: "IssueItemStockRequisitions");

            migrationBuilder.DropColumn(
                name: "ItemStockRequisitionItemId",
                table: "IssueItemStockRequisitions");
        }
    }
}
