using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class UpdateApprovals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApprovalStages_users_CreatedById",
                table: "ApprovalStages");

            migrationBuilder.DropForeignKey(
                name: "FK_ApprovalStages_users_LastDeletedById",
                table: "ApprovalStages");

            migrationBuilder.DropForeignKey(
                name: "FK_ApprovalStages_users_LastUpdatedById",
                table: "ApprovalStages");

            migrationBuilder.DropForeignKey(
                name: "FK_RequisitionApprovals_users_CreatedById",
                table: "RequisitionApprovals");

            migrationBuilder.DropForeignKey(
                name: "FK_RequisitionApprovals_users_LastDeletedById",
                table: "RequisitionApprovals");

            migrationBuilder.DropForeignKey(
                name: "FK_RequisitionApprovals_users_LastUpdatedById",
                table: "RequisitionApprovals");

            migrationBuilder.DropIndex(
                name: "IX_RequisitionApprovals_CreatedById",
                table: "RequisitionApprovals");

            migrationBuilder.DropIndex(
                name: "IX_RequisitionApprovals_LastDeletedById",
                table: "RequisitionApprovals");

            migrationBuilder.DropIndex(
                name: "IX_RequisitionApprovals_LastUpdatedById",
                table: "RequisitionApprovals");

            migrationBuilder.DropIndex(
                name: "IX_ApprovalStages_CreatedById",
                table: "ApprovalStages");

            migrationBuilder.DropIndex(
                name: "IX_ApprovalStages_LastDeletedById",
                table: "ApprovalStages");

            migrationBuilder.DropIndex(
                name: "IX_ApprovalStages_LastUpdatedById",
                table: "ApprovalStages");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "RequisitionApprovals");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "RequisitionApprovals");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "RequisitionApprovals");

            migrationBuilder.DropColumn(
                name: "LastDeletedById",
                table: "RequisitionApprovals");

            migrationBuilder.DropColumn(
                name: "LastUpdatedById",
                table: "RequisitionApprovals");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "RequisitionApprovals");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "ApprovalStages");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "ApprovalStages");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "ApprovalStages");

            migrationBuilder.DropColumn(
                name: "LastDeletedById",
                table: "ApprovalStages");

            migrationBuilder.DropColumn(
                name: "LastUpdatedById",
                table: "ApprovalStages");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ApprovalStages");

            migrationBuilder.DropColumn(
                name: "RequisitionType",
                table: "Approvals");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Sibling");

            migrationBuilder.DropColumn(
                name: "ItemType",
                table: "Approvals");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "RequisitionApprovals",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedById",
                table: "RequisitionApprovals",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "RequisitionApprovals",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LastDeletedById",
                table: "RequisitionApprovals",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LastUpdatedById",
                table: "RequisitionApprovals",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "RequisitionApprovals",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "ApprovalStages",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedById",
                table: "ApprovalStages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "ApprovalStages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LastDeletedById",
                table: "ApprovalStages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LastUpdatedById",
                table: "ApprovalStages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "ApprovalStages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RequisitionType",
                table: "Approvals",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_RequisitionApprovals_CreatedById",
                table: "RequisitionApprovals",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RequisitionApprovals_LastDeletedById",
                table: "RequisitionApprovals",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_RequisitionApprovals_LastUpdatedById",
                table: "RequisitionApprovals",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalStages_CreatedById",
                table: "ApprovalStages",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalStages_LastDeletedById",
                table: "ApprovalStages",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalStages_LastUpdatedById",
                table: "ApprovalStages",
                column: "LastUpdatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovalStages_users_CreatedById",
                table: "ApprovalStages",
                column: "CreatedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovalStages_users_LastDeletedById",
                table: "ApprovalStages",
                column: "LastDeletedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovalStages_users_LastUpdatedById",
                table: "ApprovalStages",
                column: "LastUpdatedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RequisitionApprovals_users_CreatedById",
                table: "RequisitionApprovals",
                column: "CreatedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RequisitionApprovals_users_LastDeletedById",
                table: "RequisitionApprovals",
                column: "LastDeletedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RequisitionApprovals_users_LastUpdatedById",
                table: "RequisitionApprovals",
                column: "LastUpdatedById",
                principalTable: "users",
                principalColumn: "Id");
        }
    }
}
