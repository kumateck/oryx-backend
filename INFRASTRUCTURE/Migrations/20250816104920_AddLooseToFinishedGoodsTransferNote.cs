using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddLooseToFinishedGoodsTransferNote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Loose",
                table: "FinishedGoodsTransferNotes",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "DamagedStocksLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DamagedStockId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TimeStamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
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

            migrationBuilder.CreateTable(
                name: "RecoverableItemReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecoverableItemReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecoverableItemReports_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RecoverableItemReports_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RecoverableItemReports_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_RecoverableItemReports_CreatedById",
                table: "RecoverableItemReports",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RecoverableItemReports_LastDeletedById",
                table: "RecoverableItemReports",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_RecoverableItemReports_LastUpdatedById",
                table: "RecoverableItemReports",
                column: "LastUpdatedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DamagedStocksLogs");

            migrationBuilder.DropTable(
                name: "RecoverableItemReports");

            migrationBuilder.DropColumn(
                name: "Loose",
                table: "FinishedGoodsTransferNotes");
        }
    }
}
