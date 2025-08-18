using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddReleasedByToAtr : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "ReleaseDate",
                table: "AnalyticalTestRequests",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<Guid>(
                name: "ReleasedById",
                table: "AnalyticalTestRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticalTestRequests_ReleasedById",
                table: "AnalyticalTestRequests",
                column: "ReleasedById");

            migrationBuilder.AddForeignKey(
                name: "FK_AnalyticalTestRequests_users_ReleasedById",
                table: "AnalyticalTestRequests",
                column: "ReleasedById",
                principalTable: "users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AnalyticalTestRequests_users_ReleasedById",
                table: "AnalyticalTestRequests");

            migrationBuilder.DropIndex(
                name: "IX_AnalyticalTestRequests_ReleasedById",
                table: "AnalyticalTestRequests");

            migrationBuilder.DropColumn(
                name: "ReleasedById",
                table: "AnalyticalTestRequests");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ReleaseDate",
                table: "AnalyticalTestRequests",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
        }
    }
}
