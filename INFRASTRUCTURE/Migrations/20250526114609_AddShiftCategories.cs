using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddShiftCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            
            migrationBuilder.CreateTable(
                name: "ShiftCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShiftCategories_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ShiftCategories_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ShiftCategories_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });
            
            migrationBuilder.AddColumn<Guid>(
                name: "ShiftCategoryId",
                table: "ShiftAssignments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_ShiftAssignments_ShiftCategoryId",
                table: "ShiftAssignments",
                column: "ShiftCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftCategories_CreatedById",
                table: "ShiftCategories",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftCategories_LastDeletedById",
                table: "ShiftCategories",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftCategories_LastUpdatedById",
                table: "ShiftCategories",
                column: "LastUpdatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_ShiftAssignments_ShiftCategories_ShiftCategoryId",
                table: "ShiftAssignments",
                column: "ShiftCategoryId",
                principalTable: "ShiftCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShiftAssignments_ShiftCategories_ShiftCategoryId",
                table: "ShiftAssignments");

            migrationBuilder.DropTable(
                name: "ShiftCategories");

            migrationBuilder.DropIndex(
                name: "IX_ShiftAssignments_ShiftCategoryId",
                table: "ShiftAssignments");

            migrationBuilder.DropColumn(
                name: "ShiftCategoryId",
                table: "ShiftAssignments");
        }
    }
}
