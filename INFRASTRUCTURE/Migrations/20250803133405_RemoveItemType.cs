using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class RemoveItemType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryTypes");

            migrationBuilder.RenameColumn(
                name: "ReorderRule",
                table: "Items",
                newName: "ReorderLevel");

            migrationBuilder.AddColumn<int>(
                name: "MaximumLevel",
                table: "Items",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MinimumLevel",
                table: "Items",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaximumLevel",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "MinimumLevel",
                table: "Items");

            migrationBuilder.RenameColumn(
                name: "ReorderLevel",
                table: "Items",
                newName: "ReorderRule");

            migrationBuilder.CreateTable(
                name: "InventoryTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryTypes_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InventoryTypes_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InventoryTypes_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTypes_CreatedById",
                table: "InventoryTypes",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTypes_LastDeletedById",
                table: "InventoryTypes",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTypes_LastUpdatedById",
                table: "InventoryTypes",
                column: "LastUpdatedById");
        }
    }
}
