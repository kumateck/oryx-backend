using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class RemoveItemBatchRequirements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DamagedStockBatch");

            migrationBuilder.DropTable(
                name: "DamagedStocksLogs");

            migrationBuilder.DropColumn(
                name: "HasBatch",
                table: "Items");

            migrationBuilder.CreateTable(
                name: "ItemTransactionLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TransactionType = table.Column<string>(type: "text", nullable: true),
                    ItemCode = table.Column<string>(type: "text", nullable: true),
                    Credit = table.Column<decimal>(type: "numeric", nullable: false),
                    Debit = table.Column<decimal>(type: "numeric", nullable: false),
                    ShadowHold = table.Column<decimal>(type: "numeric", nullable: true),
                    TotalBalance = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemTransactionLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemTransactionLogs_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ItemTransactionLogs_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ItemTransactionLogs_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ItemTransactionLogs_CreatedById",
                table: "ItemTransactionLogs",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ItemTransactionLogs_LastDeletedById",
                table: "ItemTransactionLogs",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ItemTransactionLogs_LastUpdatedById",
                table: "ItemTransactionLogs",
                column: "LastUpdatedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItemTransactionLogs");

            migrationBuilder.AddColumn<bool>(
                name: "HasBatch",
                table: "Items",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "DamagedStockBatch",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    BatchNumber = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DamagedStockId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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

            migrationBuilder.CreateTable(
                name: "DamagedStocksLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DamagedStockId = table.Column<Guid>(type: "uuid", nullable: false),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TimeStamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DamagedStocksLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DamagedStocksLogs_DamagedStocks_DamagedStockId",
                        column: x => x.DamagedStockId,
                        principalTable: "DamagedStocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DamagedStocksLogs_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DamagedStocksLogs_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DamagedStocksLogs_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DamagedStocksLogs_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                name: "IX_DamagedStocksLogs_CreatedById",
                table: "DamagedStocksLogs",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_DamagedStocksLogs_DamagedStockId",
                table: "DamagedStocksLogs",
                column: "DamagedStockId");

            migrationBuilder.CreateIndex(
                name: "IX_DamagedStocksLogs_LastDeletedById",
                table: "DamagedStocksLogs",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_DamagedStocksLogs_LastUpdatedById",
                table: "DamagedStocksLogs",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_DamagedStocksLogs_UserId",
                table: "DamagedStocksLogs",
                column: "UserId");
        }
    }
}
