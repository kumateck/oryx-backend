using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddDamagedStocks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DamagedStocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    DamageStatus = table.Column<int>(type: "integer", nullable: false),
                    QuantityDamaged = table.Column<int>(type: "integer", nullable: false),
                    Remarks = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DamagedStocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DamagedStocks_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DamagedStocks_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DamagedStocks_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DamagedStocks_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ItemInventoryTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MemoId = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchNumber = table.Column<string>(type: "text", nullable: true),
                    QuantityReceived = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_ItemInventoryTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemInventoryTransactions_Memos_MemoId",
                        column: x => x.MemoId,
                        principalTable: "Memos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemInventoryTransactions_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ItemInventoryTransactions_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ItemInventoryTransactions_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DamagedStockBatch",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchNumber = table.Column<string>(type: "text", nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    DamagedStockId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DamagedStockBatch", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DamagedStockBatch_DamagedStocks_DamagedStockId",
                        column: x => x.DamagedStockId,
                        principalTable: "DamagedStocks",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DamagedStockBatch_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DamagedStockBatch_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DamagedStockBatch_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });
            
            migrationBuilder.CreateIndex(
                name: "IX_DamagedStockBatch_CreatedById",
                table: "DamagedStockBatch",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_DamagedStockBatch_DamagedStockId",
                table: "DamagedStockBatch",
                column: "DamagedStockId");

            migrationBuilder.CreateIndex(
                name: "IX_DamagedStockBatch_LastDeletedById",
                table: "DamagedStockBatch",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_DamagedStockBatch_LastUpdatedById",
                table: "DamagedStockBatch",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_DamagedStocks_CreatedById",
                table: "DamagedStocks",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_DamagedStocks_ItemId",
                table: "DamagedStocks",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_DamagedStocks_LastDeletedById",
                table: "DamagedStocks",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_DamagedStocks_LastUpdatedById",
                table: "DamagedStocks",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ItemInventoryTransactions_CreatedById",
                table: "ItemInventoryTransactions",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ItemInventoryTransactions_LastDeletedById",
                table: "ItemInventoryTransactions",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ItemInventoryTransactions_LastUpdatedById",
                table: "ItemInventoryTransactions",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ItemInventoryTransactions_MemoId",
                table: "ItemInventoryTransactions",
                column: "MemoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DamagedStockBatch");

            migrationBuilder.DropTable(
                name: "ItemInventoryTransactions");

            migrationBuilder.DropTable(
                name: "DamagedStocks");

            migrationBuilder.DropColumn(
                name: "Number",
                table: "ItemStockRequisitions");
            

            migrationBuilder.AlterColumn<string>(
                name: "Justification",
                table: "ItemStockRequisitions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100000)",
                oldMaxLength: 100000,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequisitionNo",
                table: "ItemStockRequisitions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ItemStockRequisitionId",
                table: "Items",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Items_ItemStockRequisitionId",
                table: "Items",
                column: "ItemStockRequisitionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Items_ItemStockRequisitions_ItemStockRequisitionId",
                table: "Items",
                column: "ItemStockRequisitionId",
                principalTable: "ItemStockRequisitions",
                principalColumn: "Id");
        }
    }
}
