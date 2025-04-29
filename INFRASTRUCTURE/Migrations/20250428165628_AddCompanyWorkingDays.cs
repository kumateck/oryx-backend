using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyWorkingDays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CompanyWorkingDays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Day = table.Column<int>(type: "integer", nullable: false),
                    IsWorkingDay = table.Column<bool>(type: "boolean", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyWorkingDays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyWorkingDays_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CompanyWorkingDays_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CompanyWorkingDays_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyWorkingDays_CreatedById",
                table: "CompanyWorkingDays",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyWorkingDays_LastDeletedById",
                table: "CompanyWorkingDays",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyWorkingDays_LastUpdatedById",
                table: "CompanyWorkingDays",
                column: "LastUpdatedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompanyWorkingDays");
        }
    }
}
