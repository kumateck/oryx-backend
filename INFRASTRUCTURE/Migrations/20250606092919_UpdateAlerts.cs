using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAlerts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AlertType",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "ModelType",
                table: "Alerts");

            migrationBuilder.AddColumn<int[]>(
                name: "AlertTypes",
                table: "Alerts",
                type: "integer[]",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsConfigurable",
                table: "Alerts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "NotificationType",
                table: "Alerts",
                type: "integer",
                maxLength: 255,
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "AlertRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    AlertId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlertRoles_Alerts_AlertId",
                        column: x => x.AlertId,
                        principalTable: "Alerts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AlertRoles_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AlertUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AlertId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlertUsers_Alerts_AlertId",
                        column: x => x.AlertId,
                        principalTable: "Alerts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AlertUsers_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlertRoles_AlertId",
                table: "AlertRoles",
                column: "AlertId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertRoles_RoleId",
                table: "AlertRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertUsers_AlertId",
                table: "AlertUsers",
                column: "AlertId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertUsers_UserId",
                table: "AlertUsers",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlertRoles");

            migrationBuilder.DropTable(
                name: "AlertUsers");

            migrationBuilder.DropColumn(
                name: "AlertTypes",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "IsConfigurable",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "NotificationType",
                table: "Alerts");

            migrationBuilder.AddColumn<int>(
                name: "AlertType",
                table: "Alerts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ModelType",
                table: "Alerts",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);
        }
    }
}
