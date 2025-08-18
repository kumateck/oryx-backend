using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "MarketTypeId",
                table: "ProductionScheduleProducts",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MarketTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketTypes_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MarketTypes_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MarketTypes_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionScheduleProducts_MarketTypeId",
                table: "ProductionScheduleProducts",
                column: "MarketTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketTypes_CreatedById",
                table: "MarketTypes",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_MarketTypes_LastDeletedById",
                table: "MarketTypes",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_MarketTypes_LastUpdatedById",
                table: "MarketTypes",
                column: "LastUpdatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionScheduleProducts_MarketTypes_MarketTypeId",
                table: "ProductionScheduleProducts",
                column: "MarketTypeId",
                principalTable: "MarketTypes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductionScheduleProducts_MarketTypes_MarketTypeId",
                table: "ProductionScheduleProducts");

            migrationBuilder.DropTable(
                name: "MarketTypes");

            migrationBuilder.DropIndex(
                name: "IX_ProductionScheduleProducts_MarketTypeId",
                table: "ProductionScheduleProducts");

            migrationBuilder.DropColumn(
                name: "MarketTypeId",
                table: "ProductionScheduleProducts");
        }
    }
}
