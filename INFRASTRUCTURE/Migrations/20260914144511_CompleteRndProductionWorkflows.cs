using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class CompleteRndProductionWorkflows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExecutionStartedAt",
                table: "RndTechnologyTransfers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ExecutionStartedById",
                table: "RndTechnologyTransfers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProtocolApprovalComments",
                table: "RndTechnologyTransfers",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProtocolApprovedAt",
                table: "RndTechnologyTransfers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProtocolApprovedById",
                table: "RndTechnologyTransfers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TransferredAt",
                table: "RndAnalyticalMethods",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TransferredById",
                table: "RndAnalyticalMethods",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TransferredStpId",
                table: "RndAnalyticalMethods",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RndTechnologyTransfers_ExecutionStartedById",
                table: "RndTechnologyTransfers",
                column: "ExecutionStartedById");

            migrationBuilder.CreateIndex(
                name: "IX_RndTechnologyTransfers_ProtocolApprovedById",
                table: "RndTechnologyTransfers",
                column: "ProtocolApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_RndAnalyticalMethods_TransferredById",
                table: "RndAnalyticalMethods",
                column: "TransferredById");

            migrationBuilder.AddForeignKey(
                name: "FK_RndAnalyticalMethods_users_TransferredById",
                table: "RndAnalyticalMethods",
                column: "TransferredById",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RndTechnologyTransfers_users_ExecutionStartedById",
                table: "RndTechnologyTransfers",
                column: "ExecutionStartedById",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RndTechnologyTransfers_users_ProtocolApprovedById",
                table: "RndTechnologyTransfers",
                column: "ProtocolApprovedById",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RndAnalyticalMethods_users_TransferredById",
                table: "RndAnalyticalMethods");

            migrationBuilder.DropForeignKey(
                name: "FK_RndTechnologyTransfers_users_ExecutionStartedById",
                table: "RndTechnologyTransfers");

            migrationBuilder.DropForeignKey(
                name: "FK_RndTechnologyTransfers_users_ProtocolApprovedById",
                table: "RndTechnologyTransfers");

            migrationBuilder.DropIndex(
                name: "IX_RndTechnologyTransfers_ExecutionStartedById",
                table: "RndTechnologyTransfers");

            migrationBuilder.DropIndex(
                name: "IX_RndTechnologyTransfers_ProtocolApprovedById",
                table: "RndTechnologyTransfers");

            migrationBuilder.DropIndex(
                name: "IX_RndAnalyticalMethods_TransferredById",
                table: "RndAnalyticalMethods");

            migrationBuilder.DropColumn(
                name: "ExecutionStartedAt",
                table: "RndTechnologyTransfers");

            migrationBuilder.DropColumn(
                name: "ExecutionStartedById",
                table: "RndTechnologyTransfers");

            migrationBuilder.DropColumn(
                name: "ProtocolApprovalComments",
                table: "RndTechnologyTransfers");

            migrationBuilder.DropColumn(
                name: "ProtocolApprovedAt",
                table: "RndTechnologyTransfers");

            migrationBuilder.DropColumn(
                name: "ProtocolApprovedById",
                table: "RndTechnologyTransfers");

            migrationBuilder.DropColumn(
                name: "TransferredAt",
                table: "RndAnalyticalMethods");

            migrationBuilder.DropColumn(
                name: "TransferredById",
                table: "RndAnalyticalMethods");

            migrationBuilder.DropColumn(
                name: "TransferredStpId",
                table: "RndAnalyticalMethods");
        }
    }
}
