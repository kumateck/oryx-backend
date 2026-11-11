using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class LinkStockTransferIssueSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "StockTransferSourceId",
                table: "HoldingMaterialTransfers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_HoldingMaterialTransfers_StockTransferSourceId",
                table: "HoldingMaterialTransfers",
                column: "StockTransferSourceId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_HoldingMaterialTransfers_StockTransferSources_StockTransfer~",
                table: "HoldingMaterialTransfers",
                column: "StockTransferSourceId",
                principalTable: "StockTransferSources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql("""
                WITH current_rounds AS (
                    SELECT stage."ResponseId", MAX(stage."ApprovalRound") AS round
                    FROM "ResponseApprovals" stage
                    JOIN "Responses" response ON response."Id" = stage."ResponseId"
                    WHERE NOT response."Approved" AND NOT response."Rejected" AND response."DeletedAt" IS NULL
                    GROUP BY stage."ResponseId"
                ), next_required AS (
                    SELECT current_rounds."ResponseId", current_rounds.round, MIN(stage."Order") AS next_order
                    FROM current_rounds
                    JOIN "ResponseApprovals" stage ON stage."ResponseId" = current_rounds."ResponseId"
                        AND stage."ApprovalRound" = current_rounds.round
                    WHERE stage."Required" AND stage."Status" = 0
                    GROUP BY current_rounds."ResponseId", current_rounds.round
                )
                UPDATE "ResponseApprovals" stage
                SET "ActivatedAt" = CURRENT_TIMESTAMP
                FROM next_required
                WHERE stage."ResponseId" = next_required."ResponseId"
                    AND stage."ApprovalRound" = next_required.round
                    AND stage."Status" = 0 AND stage."ActivatedAt" IS NULL
                    AND (NOT stage."Required" OR stage."Order" = next_required.next_order);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Keep activation evidence because a stage may have received a decision since deployment.
            migrationBuilder.DropForeignKey(
                name: "FK_HoldingMaterialTransfers_StockTransferSources_StockTransfer~",
                table: "HoldingMaterialTransfers");

            migrationBuilder.DropIndex(
                name: "IX_HoldingMaterialTransfers_StockTransferSourceId",
                table: "HoldingMaterialTransfers");

            migrationBuilder.DropColumn(
                name: "StockTransferSourceId",
                table: "HoldingMaterialTransfers");
        }
    }
}
