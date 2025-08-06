using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class StockItemsForStockRequisition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "QuantityRequested",
                table: "ItemStockRequisitions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ItemStockRequisitionItem",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemStockRequisitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuantityRequested = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemStockRequisitionItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemStockRequisitionItem_ItemStockRequisitions_ItemStockReq~",
                        column: x => x.ItemStockRequisitionId,
                        principalTable: "ItemStockRequisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemStockRequisitionItem_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ItemStockRequisitionItem_ItemId",
                table: "ItemStockRequisitionItem",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemStockRequisitionItem_ItemStockRequisitionId",
                table: "ItemStockRequisitionItem",
                column: "ItemStockRequisitionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItemStockRequisitionItem");

            migrationBuilder.DropColumn(
                name: "QuantityRequested",
                table: "ItemStockRequisitions");
        }
    }
}
