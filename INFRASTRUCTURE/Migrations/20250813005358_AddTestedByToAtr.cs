using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddTestedByToAtr : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "TestedAt",
                table: "AnalyticalTestRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TestedById",
                table: "AnalyticalTestRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticalTestRequests_TestedById",
                table: "AnalyticalTestRequests",
                column: "TestedById");

            migrationBuilder.AddForeignKey(
                name: "FK_AnalyticalTestRequests_users_TestedById",
                table: "AnalyticalTestRequests",
                column: "TestedById",
                principalTable: "users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AnalyticalTestRequests_users_TestedById",
                table: "AnalyticalTestRequests");

            migrationBuilder.DropIndex(
                name: "IX_AnalyticalTestRequests_TestedById",
                table: "AnalyticalTestRequests");

            migrationBuilder.DropColumn(
                name: "TestedAt",
                table: "AnalyticalTestRequests");

            migrationBuilder.DropColumn(
                name: "TestedById",
                table: "AnalyticalTestRequests");
        }
    }
}
