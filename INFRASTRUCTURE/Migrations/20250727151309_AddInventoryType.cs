using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                table: "Items");

            migrationBuilder.AddColumn<Guid>(
                name: "InventoryTypeId",
                table: "Items",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "InventoryTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
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
                name: "IX_Inventories_InventoryTypeId",
                table: "Items",
                column: "InventoryTypeId");

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

            migrationBuilder.AddForeignKey(
                name: "FK_Inventories_InventoryTypes_InventoryTypeId",
                table: "Items",
                column: "InventoryTypeId",
                principalTable: "InventoryTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Inventories_InventoryTypes_InventoryTypeId",
                table: "Items");

            migrationBuilder.DropTable(
                name: "InventoryTypes");

            migrationBuilder.DropIndex(
                name: "IX_Inventories_InventoryTypeId",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "InventoryTypeId",
                table: "Items");

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Items",
                type: "text",
                nullable: true);
        }
    }
}
