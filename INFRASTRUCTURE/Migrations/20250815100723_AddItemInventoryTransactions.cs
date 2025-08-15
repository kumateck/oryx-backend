using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddItemInventoryTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "ItemInventoryTransactions");
        }
    }
}
