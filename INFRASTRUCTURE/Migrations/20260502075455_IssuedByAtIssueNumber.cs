using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class IssuedByAtIssueNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "IssuedAt",
                table: "MaterialBatches",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "IssuedById",
                table: "MaterialBatches",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "IssuedAt",
                table: "AnalyticalTestRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "IssuedById",
                table: "AnalyticalTestRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaterialBatches_IssuedById",
                table: "MaterialBatches",
                column: "IssuedById");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticalTestRequests_IssuedById",
                table: "AnalyticalTestRequests",
                column: "IssuedById");

            migrationBuilder.AddForeignKey(
                name: "FK_AnalyticalTestRequests_users_IssuedById",
                table: "AnalyticalTestRequests",
                column: "IssuedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialBatches_users_IssuedById",
                table: "MaterialBatches",
                column: "IssuedById",
                principalTable: "users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AnalyticalTestRequests_users_IssuedById",
                table: "AnalyticalTestRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_MaterialBatches_users_IssuedById",
                table: "MaterialBatches");

            migrationBuilder.DropIndex(
                name: "IX_MaterialBatches_IssuedById",
                table: "MaterialBatches");

            migrationBuilder.DropIndex(
                name: "IX_AnalyticalTestRequests_IssuedById",
                table: "AnalyticalTestRequests");

            migrationBuilder.DropColumn(
                name: "IssuedAt",
                table: "MaterialBatches");

            migrationBuilder.DropColumn(
                name: "IssuedById",
                table: "MaterialBatches");

            migrationBuilder.DropColumn(
                name: "IssuedAt",
                table: "AnalyticalTestRequests");

            migrationBuilder.DropColumn(
                name: "IssuedById",
                table: "AnalyticalTestRequests");
        }
    }
}
