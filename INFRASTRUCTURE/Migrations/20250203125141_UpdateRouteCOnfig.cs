using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRouteCOnfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Routes_WorkCenters_WorkCenterId",
                table: "Routes");

            migrationBuilder.DropTable(
                name: "RouteResponsibleParty");

            migrationBuilder.DropIndex(
                name: "IX_Routes_WorkCenterId",
                table: "Routes");

            migrationBuilder.DropColumn(
                name: "WorkCenterId",
                table: "Routes");

            migrationBuilder.CreateTable(
                name: "RouteResponsibleRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RouteId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteResponsibleRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RouteResponsibleRoles_Routes_RouteId",
                        column: x => x.RouteId,
                        principalTable: "Routes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RouteResponsibleRoles_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RouteResponsibleRoles_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RouteResponsibleRoles_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RouteResponsibleRoles_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RouteResponsibleUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RouteId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_RouteResponsibleUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RouteResponsibleUsers_Routes_RouteId",
                        column: x => x.RouteId,
                        principalTable: "Routes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RouteResponsibleUsers_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RouteResponsibleUsers_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RouteResponsibleUsers_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RouteResponsibleUsers_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RouteWorkCenters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RouteId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkCenterId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteWorkCenters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RouteWorkCenters_Routes_RouteId",
                        column: x => x.RouteId,
                        principalTable: "Routes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RouteWorkCenters_WorkCenters_WorkCenterId",
                        column: x => x.WorkCenterId,
                        principalTable: "WorkCenters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RouteWorkCenters_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RouteWorkCenters_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RouteWorkCenters_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_RouteResponsibleRoles_CreatedById",
                table: "RouteResponsibleRoles",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RouteResponsibleRoles_LastDeletedById",
                table: "RouteResponsibleRoles",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_RouteResponsibleRoles_LastUpdatedById",
                table: "RouteResponsibleRoles",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RouteResponsibleRoles_RoleId",
                table: "RouteResponsibleRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteResponsibleRoles_RouteId",
                table: "RouteResponsibleRoles",
                column: "RouteId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteResponsibleUsers_CreatedById",
                table: "RouteResponsibleUsers",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RouteResponsibleUsers_LastDeletedById",
                table: "RouteResponsibleUsers",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_RouteResponsibleUsers_LastUpdatedById",
                table: "RouteResponsibleUsers",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RouteResponsibleUsers_RouteId",
                table: "RouteResponsibleUsers",
                column: "RouteId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteResponsibleUsers_UserId",
                table: "RouteResponsibleUsers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteWorkCenters_CreatedById",
                table: "RouteWorkCenters",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RouteWorkCenters_LastDeletedById",
                table: "RouteWorkCenters",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_RouteWorkCenters_LastUpdatedById",
                table: "RouteWorkCenters",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RouteWorkCenters_RouteId",
                table: "RouteWorkCenters",
                column: "RouteId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteWorkCenters_WorkCenterId",
                table: "RouteWorkCenters",
                column: "WorkCenterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RouteResponsibleRoles");

            migrationBuilder.DropTable(
                name: "RouteResponsibleUsers");

            migrationBuilder.DropTable(
                name: "RouteWorkCenters");

            migrationBuilder.AddColumn<Guid>(
                name: "WorkCenterId",
                table: "Routes",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "RouteResponsibleParty",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: true),
                    RouteId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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
                name: "IX_Routes_WorkCenterId",
                table: "Routes",
                column: "WorkCenterId");

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
                name: "FK_Routes_WorkCenters_WorkCenterId",
                table: "Routes",
                column: "WorkCenterId",
                principalTable: "WorkCenters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
