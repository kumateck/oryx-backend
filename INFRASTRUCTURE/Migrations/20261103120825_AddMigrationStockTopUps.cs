using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddMigrationStockTopUps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Mode",
                table: "StockMigrations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "StockMigrations",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StockMigrationBalances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StockMigrationId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Before = table.Column<decimal>(type: "numeric", nullable: false),
                    Added = table.Column<decimal>(type: "numeric", nullable: false),
                    BeforeBatches = table.Column<int>(type: "integer", nullable: false),
                    AddedBatches = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMigrationBalances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockMigrationBalances_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockMigrationBalances_StockMigrations_StockMigrationId",
                        column: x => x.StockMigrationId,
                        principalTable: "StockMigrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockMigrationBalances_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockMigrationBalances_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockMigrationBalances_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockMigrationBalances_CreatedById",
                table: "StockMigrationBalances",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_StockMigrationBalances_LastDeletedById",
                table: "StockMigrationBalances",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_StockMigrationBalances_LastUpdatedById",
                table: "StockMigrationBalances",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_StockMigrationBalances_MaterialId",
                table: "StockMigrationBalances",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMigrationBalances_StockMigrationId",
                table: "StockMigrationBalances",
                column: "StockMigrationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockMigrationBalances");

            migrationBuilder.DropColumn(
                name: "Mode",
                table: "StockMigrations");

            migrationBuilder.DropColumn(
                name: "Reason",
                table: "StockMigrations");
        }
    }
}
