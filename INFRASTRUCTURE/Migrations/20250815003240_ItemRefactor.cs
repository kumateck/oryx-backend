using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class ItemRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ItemStockRequisitionItem_ItemStockRequisitions_ItemStockReq~",
                table: "ItemStockRequisitionItem");

            migrationBuilder.DropForeignKey(
                name: "FK_ItemStockRequisitionItem_Items_ItemId",
                table: "ItemStockRequisitionItem");

            migrationBuilder.DropForeignKey(
                name: "FK_ItemStockRequisitionItem_users_CreatedById",
                table: "ItemStockRequisitionItem");

            migrationBuilder.DropForeignKey(
                name: "FK_ItemStockRequisitionItem_users_LastDeletedById",
                table: "ItemStockRequisitionItem");

            migrationBuilder.DropForeignKey(
                name: "FK_ItemStockRequisitionItem_users_LastUpdatedById",
                table: "ItemStockRequisitionItem");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ItemStockRequisitionItem",
                table: "ItemStockRequisitionItem");

            migrationBuilder.RenameTable(
                name: "ItemStockRequisitionItem",
                newName: "ItemStockRequisitionItems");

            migrationBuilder.RenameIndex(
                name: "IX_ItemStockRequisitionItem_LastUpdatedById",
                table: "ItemStockRequisitionItems",
                newName: "IX_ItemStockRequisitionItems_LastUpdatedById");

            migrationBuilder.RenameIndex(
                name: "IX_ItemStockRequisitionItem_LastDeletedById",
                table: "ItemStockRequisitionItems",
                newName: "IX_ItemStockRequisitionItems_LastDeletedById");

            migrationBuilder.RenameIndex(
                name: "IX_ItemStockRequisitionItem_ItemStockRequisitionId",
                table: "ItemStockRequisitionItems",
                newName: "IX_ItemStockRequisitionItems_ItemStockRequisitionId");

            migrationBuilder.RenameIndex(
                name: "IX_ItemStockRequisitionItem_ItemId",
                table: "ItemStockRequisitionItems",
                newName: "IX_ItemStockRequisitionItems_ItemId");

            migrationBuilder.RenameIndex(
                name: "IX_ItemStockRequisitionItem_CreatedById",
                table: "ItemStockRequisitionItems",
                newName: "IX_ItemStockRequisitionItems_CreatedById");

            migrationBuilder.AddColumn<int>(
                name: "AvailableQuantity",
                table: "Items",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ItemStockRequisitionItems",
                table: "ItemStockRequisitionItems",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "IssueItemStockRequisitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemStockRequisitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuantityIssued = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssueItemStockRequisitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IssueItemStockRequisitions_ItemStockRequisitions_ItemStockR~",
                        column: x => x.ItemStockRequisitionId,
                        principalTable: "ItemStockRequisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IssueItemStockRequisitions_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_IssueItemStockRequisitions_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_IssueItemStockRequisitions_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_IssueItemStockRequisitions_CreatedById",
                table: "IssueItemStockRequisitions",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_IssueItemStockRequisitions_ItemStockRequisitionId",
                table: "IssueItemStockRequisitions",
                column: "ItemStockRequisitionId");

            migrationBuilder.CreateIndex(
                name: "IX_IssueItemStockRequisitions_LastDeletedById",
                table: "IssueItemStockRequisitions",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_IssueItemStockRequisitions_LastUpdatedById",
                table: "IssueItemStockRequisitions",
                column: "LastUpdatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_ItemStockRequisitionItems_ItemStockRequisitions_ItemStockRe~",
                table: "ItemStockRequisitionItems",
                column: "ItemStockRequisitionId",
                principalTable: "ItemStockRequisitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ItemStockRequisitionItems_Items_ItemId",
                table: "ItemStockRequisitionItems",
                column: "ItemId",
                principalTable: "Items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ItemStockRequisitionItems_users_CreatedById",
                table: "ItemStockRequisitionItems",
                column: "CreatedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ItemStockRequisitionItems_users_LastDeletedById",
                table: "ItemStockRequisitionItems",
                column: "LastDeletedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ItemStockRequisitionItems_users_LastUpdatedById",
                table: "ItemStockRequisitionItems",
                column: "LastUpdatedById",
                principalTable: "users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ItemStockRequisitionItems_ItemStockRequisitions_ItemStockRe~",
                table: "ItemStockRequisitionItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ItemStockRequisitionItems_Items_ItemId",
                table: "ItemStockRequisitionItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ItemStockRequisitionItems_users_CreatedById",
                table: "ItemStockRequisitionItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ItemStockRequisitionItems_users_LastDeletedById",
                table: "ItemStockRequisitionItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ItemStockRequisitionItems_users_LastUpdatedById",
                table: "ItemStockRequisitionItems");

            migrationBuilder.DropTable(
                name: "IssueItemStockRequisitions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ItemStockRequisitionItems",
                table: "ItemStockRequisitionItems");

            migrationBuilder.DropColumn(
                name: "AvailableQuantity",
                table: "Items");

            migrationBuilder.RenameTable(
                name: "ItemStockRequisitionItems",
                newName: "ItemStockRequisitionItem");

            migrationBuilder.RenameIndex(
                name: "IX_ItemStockRequisitionItems_LastUpdatedById",
                table: "ItemStockRequisitionItem",
                newName: "IX_ItemStockRequisitionItem_LastUpdatedById");

            migrationBuilder.RenameIndex(
                name: "IX_ItemStockRequisitionItems_LastDeletedById",
                table: "ItemStockRequisitionItem",
                newName: "IX_ItemStockRequisitionItem_LastDeletedById");

            migrationBuilder.RenameIndex(
                name: "IX_ItemStockRequisitionItems_ItemStockRequisitionId",
                table: "ItemStockRequisitionItem",
                newName: "IX_ItemStockRequisitionItem_ItemStockRequisitionId");

            migrationBuilder.RenameIndex(
                name: "IX_ItemStockRequisitionItems_ItemId",
                table: "ItemStockRequisitionItem",
                newName: "IX_ItemStockRequisitionItem_ItemId");

            migrationBuilder.RenameIndex(
                name: "IX_ItemStockRequisitionItems_CreatedById",
                table: "ItemStockRequisitionItem",
                newName: "IX_ItemStockRequisitionItem_CreatedById");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ItemStockRequisitionItem",
                table: "ItemStockRequisitionItem",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ItemStockRequisitionItem_ItemStockRequisitions_ItemStockReq~",
                table: "ItemStockRequisitionItem",
                column: "ItemStockRequisitionId",
                principalTable: "ItemStockRequisitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ItemStockRequisitionItem_Items_ItemId",
                table: "ItemStockRequisitionItem",
                column: "ItemId",
                principalTable: "Items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ItemStockRequisitionItem_users_CreatedById",
                table: "ItemStockRequisitionItem",
                column: "CreatedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ItemStockRequisitionItem_users_LastDeletedById",
                table: "ItemStockRequisitionItem",
                column: "LastDeletedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ItemStockRequisitionItem_users_LastUpdatedById",
                table: "ItemStockRequisitionItem",
                column: "LastUpdatedById",
                principalTable: "users",
                principalColumn: "Id");
        }
    }
}
