using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class LinkItemReturnsToRequisitionLines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ItemStockRequisitionItemId",
                table: "RecoverableItemReports",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecoverableItemReports_ItemStockRequisitionItemId",
                table: "RecoverableItemReports",
                column: "ItemStockRequisitionItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_RecoverableItemReports_ItemStockRequisitionItems_ItemStockR~",
                table: "RecoverableItemReports",
                column: "ItemStockRequisitionItemId",
                principalTable: "ItemStockRequisitionItems",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RecoverableItemReports_ItemStockRequisitionItems_ItemStockR~",
                table: "RecoverableItemReports");

            migrationBuilder.DropIndex(
                name: "IX_RecoverableItemReports_ItemStockRequisitionItemId",
                table: "RecoverableItemReports");

            migrationBuilder.DropColumn(
                name: "ItemStockRequisitionItemId",
                table: "RecoverableItemReports");
        }
    }
}
