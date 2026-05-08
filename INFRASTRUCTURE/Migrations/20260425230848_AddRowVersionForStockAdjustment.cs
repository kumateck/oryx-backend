using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddRowVersionForStockAdjustment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ShelfMaterialBatches",
                type: "bytea",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Items",
                type: "bytea",
                rowVersion: true,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InventoryLedgers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ReferenceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    ShelfMaterialBatchId = table.Column<Guid>(type: "uuid", nullable: true),
                    ChangeAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    PostTransactionBalance = table.Column<decimal>(type: "numeric", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryLedgers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryLedgers_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InventoryLedgers_ShelfMaterialBatches_ShelfMaterialBatchId",
                        column: x => x.ShelfMaterialBatchId,
                        principalTable: "ShelfMaterialBatches",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InventoryLedgers_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InventoryLedgers_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InventoryLedgers_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StockAdjustments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AdjustmentNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    AdjustmentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TargetType = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockAdjustments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockAdjustments_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockAdjustments_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockAdjustments_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockAdjustments_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StockAdjustmentLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StockAdjustmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    ShelfMaterialBatchId = table.Column<Guid>(type: "uuid", nullable: true),
                    PhysicalCount = table.Column<decimal>(type: "numeric", nullable: false),
                    SystemQuantitySnapshot = table.Column<decimal>(type: "numeric", nullable: false),
                    Variance = table.Column<decimal>(type: "numeric", nullable: false),
                    ReasonCode = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockAdjustmentLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockAdjustmentLines_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockAdjustmentLines_ShelfMaterialBatches_ShelfMaterialBatc~",
                        column: x => x.ShelfMaterialBatchId,
                        principalTable: "ShelfMaterialBatches",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockAdjustmentLines_StockAdjustments_StockAdjustmentId",
                        column: x => x.StockAdjustmentId,
                        principalTable: "StockAdjustments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockAdjustmentLines_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockAdjustmentLines_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockAdjustmentLines_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLedgers_CreatedById",
                table: "InventoryLedgers",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLedgers_ItemId",
                table: "InventoryLedgers",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLedgers_LastDeletedById",
                table: "InventoryLedgers",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLedgers_LastUpdatedById",
                table: "InventoryLedgers",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLedgers_ShelfMaterialBatchId",
                table: "InventoryLedgers",
                column: "ShelfMaterialBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustmentLines_CreatedById",
                table: "StockAdjustmentLines",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustmentLines_ItemId",
                table: "StockAdjustmentLines",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustmentLines_LastDeletedById",
                table: "StockAdjustmentLines",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustmentLines_LastUpdatedById",
                table: "StockAdjustmentLines",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustmentLines_ShelfMaterialBatchId",
                table: "StockAdjustmentLines",
                column: "ShelfMaterialBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustmentLines_StockAdjustmentId",
                table: "StockAdjustmentLines",
                column: "StockAdjustmentId");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustments_CreatedById",
                table: "StockAdjustments",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustments_LastDeletedById",
                table: "StockAdjustments",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustments_LastUpdatedById",
                table: "StockAdjustments",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustments_WarehouseId",
                table: "StockAdjustments",
                column: "WarehouseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryLedgers");

            migrationBuilder.DropTable(
                name: "StockAdjustmentLines");

            migrationBuilder.DropTable(
                name: "StockAdjustments");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ShelfMaterialBatches");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Items");
        }
    }
}
