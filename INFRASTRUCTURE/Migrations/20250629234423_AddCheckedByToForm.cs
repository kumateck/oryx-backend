using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddCheckedByToForm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CheckedAt",
                table: "Responses",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CheckedById",
                table: "Responses",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Responses_CheckedById",
                table: "Responses",
                column: "CheckedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Responses_users_CheckedById",
                table: "Responses",
                column: "CheckedById",
                principalTable: "users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Responses_users_CheckedById",
                table: "Responses");

            migrationBuilder.DropIndex(
                name: "IX_Responses_CheckedById",
                table: "Responses");

            migrationBuilder.DropColumn(
                name: "CheckedAt",
                table: "Responses");

            migrationBuilder.DropColumn(
                name: "CheckedById",
                table: "Responses");
        }
    }
}
