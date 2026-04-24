using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignedToATR : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.CreateTable(
                name: "AnalyticalTestRequestAssignee",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AnalyticalTestRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalyticalTestRequestAssignee", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnalyticalTestRequestAssignee_AnalyticalTestRequests_Analyt~",
                        column: x => x.AnalyticalTestRequestId,
                        principalTable: "AnalyticalTestRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AnalyticalTestRequestAssignee_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AnalyticalTestRequestAssignee_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AnalyticalTestRequestAssignee_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AnalyticalTestRequestAssignee_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticalTestRequestAssignee_AnalyticalTestRequestId",
                table: "AnalyticalTestRequestAssignee",
                column: "AnalyticalTestRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticalTestRequestAssignee_CreatedById",
                table: "AnalyticalTestRequestAssignee",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticalTestRequestAssignee_LastDeletedById",
                table: "AnalyticalTestRequestAssignee",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticalTestRequestAssignee_LastUpdatedById",
                table: "AnalyticalTestRequestAssignee",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticalTestRequestAssignee_UserId",
                table: "AnalyticalTestRequestAssignee",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnalyticalTestRequestAssignee");
            
        }
    }
}
