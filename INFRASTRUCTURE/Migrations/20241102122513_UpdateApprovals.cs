using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class UpdateApprovals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovalTime",
                table: "ApprovalStages");

            migrationBuilder.DropColumn(
                name: "Comments",
                table: "ApprovalStages");

            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "ApprovalStages");

            migrationBuilder.DropColumn(
                name: "ItemType",
                table: "Approvals");

            migrationBuilder.AddColumn<Guid>(
                name: "UomId",
                table: "RequisitionItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RequisitionType",
                table: "Approvals",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_RequisitionItems_UomId",
                table: "RequisitionItems",
                column: "UomId");

            migrationBuilder.AddForeignKey(
                name: "FK_RequisitionItems_UnitOfMeasures_UomId",
                table: "RequisitionItems",
                column: "UomId",
                principalTable: "UnitOfMeasures",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RequisitionItems_UnitOfMeasures_UomId",
                table: "RequisitionItems");

            migrationBuilder.DropIndex(
                name: "IX_RequisitionItems_UomId",
                table: "RequisitionItems");

            migrationBuilder.DropColumn(
                name: "UomId",
                table: "RequisitionItems");

            migrationBuilder.DropColumn(
                name: "RequisitionType",
                table: "Approvals");

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovalTime",
                table: "ApprovalStages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Comments",
                table: "ApprovalStages",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "ApprovalStages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ItemType",
                table: "Approvals",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);
        }
    }
}
