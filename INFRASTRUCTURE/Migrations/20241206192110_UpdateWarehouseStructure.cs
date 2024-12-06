using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class UpdateWarehouseStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "WarehouseLocations",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FloorName",
                table: "WarehouseLocations",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WarehouseLocationRacks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseLocationId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_WarehouseLocationRacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehouseLocationRacks_WarehouseLocations_WarehouseLocation~",
                        column: x => x.WarehouseLocationId,
                        principalTable: "WarehouseLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WarehouseLocationRacks_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WarehouseLocationRacks_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WarehouseLocationRacks_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WarehouseLocationShelves",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseLocationRackId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
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
                    table.PrimaryKey("PK_WarehouseLocationShelves", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehouseLocationShelves_WarehouseLocationRacks_WarehouseLo~",
                        column: x => x.WarehouseLocationRackId,
                        principalTable: "WarehouseLocationRacks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WarehouseLocationShelves_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WarehouseLocationShelves_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WarehouseLocationShelves_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseLocationRacks_CreatedById",
                table: "WarehouseLocationRacks",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseLocationRacks_LastDeletedById",
                table: "WarehouseLocationRacks",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseLocationRacks_LastUpdatedById",
                table: "WarehouseLocationRacks",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseLocationRacks_WarehouseLocationId",
                table: "WarehouseLocationRacks",
                column: "WarehouseLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseLocationShelves_CreatedById",
                table: "WarehouseLocationShelves",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseLocationShelves_LastDeletedById",
                table: "WarehouseLocationShelves",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseLocationShelves_LastUpdatedById",
                table: "WarehouseLocationShelves",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseLocationShelves_WarehouseLocationRackId",
                table: "WarehouseLocationShelves",
                column: "WarehouseLocationRackId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WarehouseLocationShelves");

            migrationBuilder.DropTable(
                name: "WarehouseLocationRacks");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "WarehouseLocations");

            migrationBuilder.DropColumn(
                name: "FloorName",
                table: "WarehouseLocations");
        }
    }
}
