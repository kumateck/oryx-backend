using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class ModifyJobOrderServiceProvider : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "JobOrderServiceProviders",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedById",
                table: "JobOrderServiceProviders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "JobOrderServiceProviders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LastDeletedById",
                table: "JobOrderServiceProviders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LastUpdatedById",
                table: "JobOrderServiceProviders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "JobOrderServiceProviders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobOrderServiceProviders_CreatedById",
                table: "JobOrderServiceProviders",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_JobOrderServiceProviders_LastDeletedById",
                table: "JobOrderServiceProviders",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_JobOrderServiceProviders_LastUpdatedById",
                table: "JobOrderServiceProviders",
                column: "LastUpdatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_JobOrderServiceProviders_users_CreatedById",
                table: "JobOrderServiceProviders",
                column: "CreatedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_JobOrderServiceProviders_users_LastDeletedById",
                table: "JobOrderServiceProviders",
                column: "LastDeletedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_JobOrderServiceProviders_users_LastUpdatedById",
                table: "JobOrderServiceProviders",
                column: "LastUpdatedById",
                principalTable: "users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JobOrderServiceProviders_users_CreatedById",
                table: "JobOrderServiceProviders");

            migrationBuilder.DropForeignKey(
                name: "FK_JobOrderServiceProviders_users_LastDeletedById",
                table: "JobOrderServiceProviders");

            migrationBuilder.DropForeignKey(
                name: "FK_JobOrderServiceProviders_users_LastUpdatedById",
                table: "JobOrderServiceProviders");

            migrationBuilder.DropIndex(
                name: "IX_JobOrderServiceProviders_CreatedById",
                table: "JobOrderServiceProviders");

            migrationBuilder.DropIndex(
                name: "IX_JobOrderServiceProviders_LastDeletedById",
                table: "JobOrderServiceProviders");

            migrationBuilder.DropIndex(
                name: "IX_JobOrderServiceProviders_LastUpdatedById",
                table: "JobOrderServiceProviders");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "JobOrderServiceProviders");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "JobOrderServiceProviders");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "JobOrderServiceProviders");

            migrationBuilder.DropColumn(
                name: "LastDeletedById",
                table: "JobOrderServiceProviders");

            migrationBuilder.DropColumn(
                name: "LastUpdatedById",
                table: "JobOrderServiceProviders");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "JobOrderServiceProviders");
        }
    }
}
