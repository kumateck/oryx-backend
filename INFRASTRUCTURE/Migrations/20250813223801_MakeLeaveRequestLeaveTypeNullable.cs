using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class MakeLeaveRequestLeaveTypeNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LeaveRequests_LeaveTypes_LeaveTypeId",
                table: "LeaveRequests");

            migrationBuilder.AlterColumn<Guid>(
                name: "LeaveTypeId",
                table: "LeaveRequests",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "ItemStockRequisitionItem",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedById",
                table: "ItemStockRequisitionItem",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "ItemStockRequisitionItem",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LastDeletedById",
                table: "ItemStockRequisitionItem",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LastUpdatedById",
                table: "ItemStockRequisitionItem",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "ItemStockRequisitionItem",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemStockRequisitionItem_CreatedById",
                table: "ItemStockRequisitionItem",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ItemStockRequisitionItem_LastDeletedById",
                table: "ItemStockRequisitionItem",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ItemStockRequisitionItem_LastUpdatedById",
                table: "ItemStockRequisitionItem",
                column: "LastUpdatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_ItemStockRequisitionItem_users_CreatedById",
                table: "ItemStockRequisitionItem",
                column: "CreatedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ItemStockRequisitionItem_users_LastDeletedById",
                table: "ItemStockRequisitionItem",
                column: "LastDeletedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ItemStockRequisitionItem_users_LastUpdatedById",
                table: "ItemStockRequisitionItem",
                column: "LastUpdatedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveRequests_LeaveTypes_LeaveTypeId",
                table: "LeaveRequests",
                column: "LeaveTypeId",
                principalTable: "LeaveTypes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ItemStockRequisitionItem_users_CreatedById",
                table: "ItemStockRequisitionItem");

            migrationBuilder.DropForeignKey(
                name: "FK_ItemStockRequisitionItem_users_LastDeletedById",
                table: "ItemStockRequisitionItem");

            migrationBuilder.DropForeignKey(
                name: "FK_ItemStockRequisitionItem_users_LastUpdatedById",
                table: "ItemStockRequisitionItem");

            migrationBuilder.DropForeignKey(
                name: "FK_LeaveRequests_LeaveTypes_LeaveTypeId",
                table: "LeaveRequests");

            migrationBuilder.DropIndex(
                name: "IX_ItemStockRequisitionItem_CreatedById",
                table: "ItemStockRequisitionItem");

            migrationBuilder.DropIndex(
                name: "IX_ItemStockRequisitionItem_LastDeletedById",
                table: "ItemStockRequisitionItem");

            migrationBuilder.DropIndex(
                name: "IX_ItemStockRequisitionItem_LastUpdatedById",
                table: "ItemStockRequisitionItem");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "ItemStockRequisitionItem");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "ItemStockRequisitionItem");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "ItemStockRequisitionItem");

            migrationBuilder.DropColumn(
                name: "LastDeletedById",
                table: "ItemStockRequisitionItem");

            migrationBuilder.DropColumn(
                name: "LastUpdatedById",
                table: "ItemStockRequisitionItem");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ItemStockRequisitionItem");

            migrationBuilder.AlterColumn<Guid>(
                name: "LeaveTypeId",
                table: "LeaveRequests",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveRequests_LeaveTypes_LeaveTypeId",
                table: "LeaveRequests",
                column: "LeaveTypeId",
                principalTable: "LeaveTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
