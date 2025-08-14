using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class RemoveReleasedAtFromAtr : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReleaseDate",
                table: "AnalyticalTestRequests");

            migrationBuilder.DropColumn(
                name: "ReleasedAt",
                table: "AnalyticalTestRequests");

            migrationBuilder.AddColumn<string>(
                name: "ArNumber",
                table: "AnalyticalTestRequests",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArNumber",
                table: "AnalyticalTestRequests");

            migrationBuilder.AddColumn<DateTime>(
                name: "ReleaseDate",
                table: "AnalyticalTestRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReleasedAt",
                table: "AnalyticalTestRequests",
                type: "text",
                nullable: true);
        }
    }
}
