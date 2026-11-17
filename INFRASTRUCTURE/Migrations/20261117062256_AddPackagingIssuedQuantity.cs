using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddPackagingIssuedQuantity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "IssueAdjustedAt",
                table: "RequisitionItems",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "IssueAdjustedById",
                table: "RequisitionItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IssueAdjustmentReason",
                table: "RequisitionItems",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "IssuedQuantity",
                table: "RequisitionItems",
                type: "numeric",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RequisitionItems_IssueAdjustedById",
                table: "RequisitionItems",
                column: "IssueAdjustedById");

            migrationBuilder.AddForeignKey(
                name: "FK_RequisitionItems_users_IssueAdjustedById",
                table: "RequisitionItems",
                column: "IssueAdjustedById",
                principalTable: "users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RequisitionItems_users_IssueAdjustedById",
                table: "RequisitionItems");

            migrationBuilder.DropIndex(
                name: "IX_RequisitionItems_IssueAdjustedById",
                table: "RequisitionItems");

            migrationBuilder.DropColumn(
                name: "IssueAdjustedAt",
                table: "RequisitionItems");

            migrationBuilder.DropColumn(
                name: "IssueAdjustedById",
                table: "RequisitionItems");

            migrationBuilder.DropColumn(
                name: "IssueAdjustmentReason",
                table: "RequisitionItems");

            migrationBuilder.DropColumn(
                name: "IssuedQuantity",
                table: "RequisitionItems");
        }
    }
}
