using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class ShipmentDiscrepancyTypeFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscrepancyType",
                table: "ShipmentDiscrepancyItem");

            migrationBuilder.AddColumn<Guid>(
                name: "TypeId",
                table: "ShipmentDiscrepancyItem",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ShipmentDiscrepancyTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipmentDiscrepancyTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShipmentDiscrepancyTypes_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ShipmentDiscrepancyTypes_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ShipmentDiscrepancyTypes_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentDiscrepancyItem_TypeId",
                table: "ShipmentDiscrepancyItem",
                column: "TypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentDiscrepancyTypes_CreatedById",
                table: "ShipmentDiscrepancyTypes",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentDiscrepancyTypes_LastDeletedById",
                table: "ShipmentDiscrepancyTypes",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentDiscrepancyTypes_LastUpdatedById",
                table: "ShipmentDiscrepancyTypes",
                column: "LastUpdatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_ShipmentDiscrepancyItem_ShipmentDiscrepancyTypes_TypeId",
                table: "ShipmentDiscrepancyItem",
                column: "TypeId",
                principalTable: "ShipmentDiscrepancyTypes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShipmentDiscrepancyItem_ShipmentDiscrepancyTypes_TypeId",
                table: "ShipmentDiscrepancyItem");

            migrationBuilder.DropTable(
                name: "ShipmentDiscrepancyTypes");

            migrationBuilder.DropIndex(
                name: "IX_ShipmentDiscrepancyItem_TypeId",
                table: "ShipmentDiscrepancyItem");

            migrationBuilder.DropColumn(
                name: "TypeId",
                table: "ShipmentDiscrepancyItem");

            migrationBuilder.AddColumn<int>(
                name: "DiscrepancyType",
                table: "ShipmentDiscrepancyItem",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
