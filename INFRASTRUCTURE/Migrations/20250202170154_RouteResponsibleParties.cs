using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class RouteResponsibleParties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "WorkflowId",
                table: "Routes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledEndTime",
                table: "ProductionScheduleProducts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledStartTime",
                table: "ProductionScheduleProducts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "RouteResponsibleParty",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RouteId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteResponsibleParty", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RouteResponsibleParty_Routes_RouteId",
                        column: x => x.RouteId,
                        principalTable: "Routes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RouteResponsibleParty_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RouteResponsibleParty_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RouteResponsibleParty_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RouteResponsibleParty_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RouteResponsibleParty_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Routes_WorkflowId",
                table: "Routes",
                column: "WorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteResponsibleParty_CreatedById",
                table: "RouteResponsibleParty",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RouteResponsibleParty_LastDeletedById",
                table: "RouteResponsibleParty",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_RouteResponsibleParty_LastUpdatedById",
                table: "RouteResponsibleParty",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RouteResponsibleParty_RoleId",
                table: "RouteResponsibleParty",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteResponsibleParty_RouteId",
                table: "RouteResponsibleParty",
                column: "RouteId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteResponsibleParty_UserId",
                table: "RouteResponsibleParty",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Routes_Forms_WorkflowId",
                table: "Routes",
                column: "WorkflowId",
                principalTable: "Forms",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Routes_Forms_WorkflowId",
                table: "Routes");

            migrationBuilder.DropTable(
                name: "RouteResponsibleParty");

            migrationBuilder.DropIndex(
                name: "IX_Routes_WorkflowId",
                table: "Routes");

            migrationBuilder.DropColumn(
                name: "WorkflowId",
                table: "Routes");

            migrationBuilder.DropColumn(
                name: "ScheduledEndTime",
                table: "ProductionScheduleProducts");

            migrationBuilder.DropColumn(
                name: "ScheduledStartTime",
                table: "ProductionScheduleProducts");
        }
    }
}
