using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class LinkEmployeesToUsersAndInvitations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Employees",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EmployeeInvitations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    Name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    EmployeeType = table.Column<int>(type: "integer", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeInvitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeInvitations_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmployeeInvitations_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmployeeInvitations_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Employees_UserId",
                table: "Employees",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeInvitations_CreatedById",
                table: "EmployeeInvitations",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeInvitations_LastDeletedById",
                table: "EmployeeInvitations",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeInvitations_LastUpdatedById",
                table: "EmployeeInvitations",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeInvitations_TokenHash",
                table: "EmployeeInvitations",
                column: "TokenHash",
                unique: true);

            // Link existing accounts to employees only where the email identifies exactly one live employee and exactly
            // one live account; anything ambiguous stays unlinked for HR to resolve.
            migrationBuilder.Sql("""
                UPDATE "Employees" AS e
                SET "UserId" = u."Id"
                FROM "users" AS u
                WHERE e."UserId" IS NULL
                  AND e."DeletedAt" IS NULL AND u."DeletedAt" IS NULL
                  AND COALESCE(TRIM(e."Email"), '') <> ''
                  AND LOWER(TRIM(u."Email")) = LOWER(TRIM(e."Email"))
                  AND (SELECT COUNT(*) FROM "Employees" AS e2
                       WHERE e2."DeletedAt" IS NULL AND LOWER(TRIM(e2."Email")) = LOWER(TRIM(e."Email"))) = 1
                  AND (SELECT COUNT(*) FROM "users" AS u2
                       WHERE u2."DeletedAt" IS NULL AND LOWER(TRIM(u2."Email")) = LOWER(TRIM(e."Email"))) = 1;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeeInvitations");

            migrationBuilder.DropIndex(
                name: "IX_Employees_UserId",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Employees");
        }
    }
}
