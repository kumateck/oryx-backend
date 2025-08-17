using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePOrderAndFNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ItemId",
                table: "RecoverableItemReports",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "RecoverableItemReports",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "RecoverableItemReports",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Fulfilled",
                table: "ProductionOrderProducts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "AllocatedQuantity",
                table: "FinishedGoodsTransferNotes",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "ProductionOrderProductQuantity",
                columns: table => new
                {
                    ProductionOrderProductsProductionOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionOrderProductsId = table.Column<int>(type: "integer", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FinishedGoodsTransferNoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionOrderProductQuantity", x => new { x.ProductionOrderProductsProductionOrderId, x.ProductionOrderProductsId, x.Id });
                    table.ForeignKey(
                        name: "FK_ProductionOrderProductQuantity_FinishedGoodsTransferNotes_F~",
                        column: x => x.FinishedGoodsTransferNoteId,
                        principalTable: "FinishedGoodsTransferNotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionOrderProductQuantity_ProductionOrderProducts_Prod~",
                        columns: x => new { x.ProductionOrderProductsProductionOrderId, x.ProductionOrderProductsId },
                        principalTable: "ProductionOrderProducts",
                        principalColumns: new[] { "ProductionOrderId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecoverableItemReports_ItemId",
                table: "RecoverableItemReports",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrderProductQuantity_FinishedGoodsTransferNoteId",
                table: "ProductionOrderProductQuantity",
                column: "FinishedGoodsTransferNoteId");

            migrationBuilder.AddForeignKey(
                name: "FK_RecoverableItemReports_Items_ItemId",
                table: "RecoverableItemReports",
                column: "ItemId",
                principalTable: "Items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RecoverableItemReports_Items_ItemId",
                table: "RecoverableItemReports");

            migrationBuilder.DropTable(
                name: "ProductionOrderProductQuantity");

            migrationBuilder.DropIndex(
                name: "IX_RecoverableItemReports_ItemId",
                table: "RecoverableItemReports");

            migrationBuilder.DropColumn(
                name: "ItemId",
                table: "RecoverableItemReports");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "RecoverableItemReports");

            migrationBuilder.DropColumn(
                name: "Reason",
                table: "RecoverableItemReports");

            migrationBuilder.DropColumn(
                name: "Fulfilled",
                table: "ProductionOrderProducts");

            migrationBuilder.DropColumn(
                name: "AllocatedQuantity",
                table: "FinishedGoodsTransferNotes");
        }
    }
}
