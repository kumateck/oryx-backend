using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class MakeMaterialReturnNoteReturnBaseEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItemInventoryTransactions");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "MaterialReturnNotePartialReturns",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedById",
                table: "MaterialReturnNotePartialReturns",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "MaterialReturnNotePartialReturns",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LastDeletedById",
                table: "MaterialReturnNotePartialReturns",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LastUpdatedById",
                table: "MaterialReturnNotePartialReturns",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "MaterialReturnNotePartialReturns",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "MaterialReturnNoteFullReturns",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedById",
                table: "MaterialReturnNoteFullReturns",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "MaterialReturnNoteFullReturns",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LastDeletedById",
                table: "MaterialReturnNoteFullReturns",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LastUpdatedById",
                table: "MaterialReturnNoteFullReturns",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "MaterialReturnNoteFullReturns",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BatchNumber",
                table: "Items",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaterialReturnNotePartialReturns_CreatedById",
                table: "MaterialReturnNotePartialReturns",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialReturnNotePartialReturns_LastDeletedById",
                table: "MaterialReturnNotePartialReturns",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialReturnNotePartialReturns_LastUpdatedById",
                table: "MaterialReturnNotePartialReturns",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialReturnNoteFullReturns_CreatedById",
                table: "MaterialReturnNoteFullReturns",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialReturnNoteFullReturns_LastDeletedById",
                table: "MaterialReturnNoteFullReturns",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialReturnNoteFullReturns_LastUpdatedById",
                table: "MaterialReturnNoteFullReturns",
                column: "LastUpdatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialReturnNoteFullReturns_users_CreatedById",
                table: "MaterialReturnNoteFullReturns",
                column: "CreatedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialReturnNoteFullReturns_users_LastDeletedById",
                table: "MaterialReturnNoteFullReturns",
                column: "LastDeletedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialReturnNoteFullReturns_users_LastUpdatedById",
                table: "MaterialReturnNoteFullReturns",
                column: "LastUpdatedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialReturnNotePartialReturns_users_CreatedById",
                table: "MaterialReturnNotePartialReturns",
                column: "CreatedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialReturnNotePartialReturns_users_LastDeletedById",
                table: "MaterialReturnNotePartialReturns",
                column: "LastDeletedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialReturnNotePartialReturns_users_LastUpdatedById",
                table: "MaterialReturnNotePartialReturns",
                column: "LastUpdatedById",
                principalTable: "users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialReturnNoteFullReturns_users_CreatedById",
                table: "MaterialReturnNoteFullReturns");

            migrationBuilder.DropForeignKey(
                name: "FK_MaterialReturnNoteFullReturns_users_LastDeletedById",
                table: "MaterialReturnNoteFullReturns");

            migrationBuilder.DropForeignKey(
                name: "FK_MaterialReturnNoteFullReturns_users_LastUpdatedById",
                table: "MaterialReturnNoteFullReturns");

            migrationBuilder.DropForeignKey(
                name: "FK_MaterialReturnNotePartialReturns_users_CreatedById",
                table: "MaterialReturnNotePartialReturns");

            migrationBuilder.DropForeignKey(
                name: "FK_MaterialReturnNotePartialReturns_users_LastDeletedById",
                table: "MaterialReturnNotePartialReturns");

            migrationBuilder.DropForeignKey(
                name: "FK_MaterialReturnNotePartialReturns_users_LastUpdatedById",
                table: "MaterialReturnNotePartialReturns");

            migrationBuilder.DropIndex(
                name: "IX_MaterialReturnNotePartialReturns_CreatedById",
                table: "MaterialReturnNotePartialReturns");

            migrationBuilder.DropIndex(
                name: "IX_MaterialReturnNotePartialReturns_LastDeletedById",
                table: "MaterialReturnNotePartialReturns");

            migrationBuilder.DropIndex(
                name: "IX_MaterialReturnNotePartialReturns_LastUpdatedById",
                table: "MaterialReturnNotePartialReturns");

            migrationBuilder.DropIndex(
                name: "IX_MaterialReturnNoteFullReturns_CreatedById",
                table: "MaterialReturnNoteFullReturns");

            migrationBuilder.DropIndex(
                name: "IX_MaterialReturnNoteFullReturns_LastDeletedById",
                table: "MaterialReturnNoteFullReturns");

            migrationBuilder.DropIndex(
                name: "IX_MaterialReturnNoteFullReturns_LastUpdatedById",
                table: "MaterialReturnNoteFullReturns");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "MaterialReturnNotePartialReturns");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "MaterialReturnNotePartialReturns");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "MaterialReturnNotePartialReturns");

            migrationBuilder.DropColumn(
                name: "LastDeletedById",
                table: "MaterialReturnNotePartialReturns");

            migrationBuilder.DropColumn(
                name: "LastUpdatedById",
                table: "MaterialReturnNotePartialReturns");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "MaterialReturnNotePartialReturns");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "MaterialReturnNoteFullReturns");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "MaterialReturnNoteFullReturns");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "MaterialReturnNoteFullReturns");

            migrationBuilder.DropColumn(
                name: "LastDeletedById",
                table: "MaterialReturnNoteFullReturns");

            migrationBuilder.DropColumn(
                name: "LastUpdatedById",
                table: "MaterialReturnNoteFullReturns");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "MaterialReturnNoteFullReturns");

            migrationBuilder.DropColumn(
                name: "BatchNumber",
                table: "Items");

            migrationBuilder.CreateTable(
                name: "ItemInventoryTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    MemoId = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchNumber = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    QuantityIssued = table.Column<int>(type: "integer", nullable: false),
                    QuantityReceived = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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
    }
}
