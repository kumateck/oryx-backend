using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddEscalationToApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ActivatedAt",
                table: "RequisitionApprovals",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ApprovalId",
                table: "RequisitionApprovals",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "RequisitionApprovals",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "StageStartTime",
                table: "RequisitionApprovals",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ActivatedAt",
                table: "PurchaseOrderApprovals",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ApprovalId",
                table: "PurchaseOrderApprovals",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "PurchaseOrderApprovals",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "StageStartTime",
                table: "PurchaseOrderApprovals",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ActivatedAt",
                table: "BillingSheetApprovals",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ApprovalId",
                table: "BillingSheetApprovals",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "BillingSheetApprovals",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "StageStartTime",
                table: "BillingSheetApprovals",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "EscalationDuration",
                table: "Approvals",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.CreateIndex(
                name: "IX_RequisitionApprovals_ApprovalId",
                table: "RequisitionApprovals",
                column: "ApprovalId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderApprovals_ApprovalId",
                table: "PurchaseOrderApprovals",
                column: "ApprovalId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingSheetApprovals_ApprovalId",
                table: "BillingSheetApprovals",
                column: "ApprovalId");

            migrationBuilder.AddForeignKey(
                name: "FK_BillingSheetApprovals_Approvals_ApprovalId",
                table: "BillingSheetApprovals",
                column: "ApprovalId",
                principalTable: "Approvals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrderApprovals_Approvals_ApprovalId",
                table: "PurchaseOrderApprovals",
                column: "ApprovalId",
                principalTable: "Approvals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RequisitionApprovals_Approvals_ApprovalId",
                table: "RequisitionApprovals",
                column: "ApprovalId",
                principalTable: "Approvals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BillingSheetApprovals_Approvals_ApprovalId",
                table: "BillingSheetApprovals");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrderApprovals_Approvals_ApprovalId",
                table: "PurchaseOrderApprovals");

            migrationBuilder.DropForeignKey(
                name: "FK_RequisitionApprovals_Approvals_ApprovalId",
                table: "RequisitionApprovals");

            migrationBuilder.DropIndex(
                name: "IX_RequisitionApprovals_ApprovalId",
                table: "RequisitionApprovals");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrderApprovals_ApprovalId",
                table: "PurchaseOrderApprovals");

            migrationBuilder.DropIndex(
                name: "IX_BillingSheetApprovals_ApprovalId",
                table: "BillingSheetApprovals");

            migrationBuilder.DropColumn(
                name: "ActivatedAt",
                table: "RequisitionApprovals");

            migrationBuilder.DropColumn(
                name: "ApprovalId",
                table: "RequisitionApprovals");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "RequisitionApprovals");

            migrationBuilder.DropColumn(
                name: "StageStartTime",
                table: "RequisitionApprovals");

            migrationBuilder.DropColumn(
                name: "ActivatedAt",
                table: "PurchaseOrderApprovals");

            migrationBuilder.DropColumn(
                name: "ApprovalId",
                table: "PurchaseOrderApprovals");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "PurchaseOrderApprovals");

            migrationBuilder.DropColumn(
                name: "StageStartTime",
                table: "PurchaseOrderApprovals");

            migrationBuilder.DropColumn(
                name: "ActivatedAt",
                table: "BillingSheetApprovals");

            migrationBuilder.DropColumn(
                name: "ApprovalId",
                table: "BillingSheetApprovals");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "BillingSheetApprovals");

            migrationBuilder.DropColumn(
                name: "StageStartTime",
                table: "BillingSheetApprovals");

            migrationBuilder.DropColumn(
                name: "EscalationDuration",
                table: "Approvals");
        }
    }
}
