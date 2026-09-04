using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkingHoursPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkingHoursPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MaxHoursPerDay = table.Column<decimal>(type: "numeric", nullable: false),
                    MaxHoursPerWeek = table.Column<decimal>(type: "numeric", nullable: false),
                    MinDailyRestHours = table.Column<decimal>(type: "numeric", nullable: false),
                    MinWeeklyRestHours = table.Column<decimal>(type: "numeric", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkingHoursPolicies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkingHoursPolicies_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WorkingHoursPolicies_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WorkingHoursPolicies_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkingHoursPolicies_CreatedById",
                table: "WorkingHoursPolicies",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_WorkingHoursPolicies_LastDeletedById",
                table: "WorkingHoursPolicies",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_WorkingHoursPolicies_LastUpdatedById",
                table: "WorkingHoursPolicies",
                column: "LastUpdatedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkingHoursPolicies");
        }
    }
}
