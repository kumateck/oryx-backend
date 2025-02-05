using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddActivityLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductionActivityLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionActivityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionActivityLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionActivityLogs_ProductionActivities_ProductionActiv~",
                        column: x => x.ProductionActivityId,
                        principalTable: "ProductionActivities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionActivityLogs_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionActivityLogs_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionActivityLogs_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionActivityLogs_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionActivityLogs_CreatedById",
                table: "ProductionActivityLogs",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionActivityLogs_LastDeletedById",
                table: "ProductionActivityLogs",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionActivityLogs_LastUpdatedById",
                table: "ProductionActivityLogs",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionActivityLogs_ProductionActivityId",
                table: "ProductionActivityLogs",
                column: "ProductionActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionActivityLogs_UserId",
                table: "ProductionActivityLogs",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductionActivityLogs");
        }
    }
}
