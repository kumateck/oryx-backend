using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAtr : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NumberOfContainers",
                table: "AnalyticalTestRequests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "SampledAt",
                table: "AnalyticalTestRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SampledById",
                table: "AnalyticalTestRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticalTestRequests_SampledById",
                table: "AnalyticalTestRequests",
                column: "SampledById");

            migrationBuilder.AddForeignKey(
                name: "FK_AnalyticalTestRequests_users_SampledById",
                table: "AnalyticalTestRequests",
                column: "SampledById",
                principalTable: "users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AnalyticalTestRequests_users_SampledById",
                table: "AnalyticalTestRequests");

            migrationBuilder.DropIndex(
                name: "IX_AnalyticalTestRequests_SampledById",
                table: "AnalyticalTestRequests");

            migrationBuilder.DropColumn(
                name: "NumberOfContainers",
                table: "AnalyticalTestRequests");

            migrationBuilder.DropColumn(
                name: "SampledAt",
                table: "AnalyticalTestRequests");

            migrationBuilder.DropColumn(
                name: "SampledById",
                table: "AnalyticalTestRequests");
        }
    }
}
