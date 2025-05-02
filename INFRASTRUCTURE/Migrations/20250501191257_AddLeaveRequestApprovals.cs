using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaveRequestApprovals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LeaveRequestApproval_Approvals_ApprovalId",
                table: "LeaveRequestApproval");

            migrationBuilder.DropForeignKey(
                name: "FK_LeaveRequestApproval_LeaveRequests_LeaveRequestId",
                table: "LeaveRequestApproval");

            migrationBuilder.DropForeignKey(
                name: "FK_LeaveRequestApproval_roles_RoleId",
                table: "LeaveRequestApproval");

            migrationBuilder.DropForeignKey(
                name: "FK_LeaveRequestApproval_users_ApprovedById",
                table: "LeaveRequestApproval");

            migrationBuilder.DropForeignKey(
                name: "FK_LeaveRequestApproval_users_UserId",
                table: "LeaveRequestApproval");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LeaveRequestApproval",
                table: "LeaveRequestApproval");

            migrationBuilder.RenameTable(
                name: "LeaveRequestApproval",
                newName: "LeaveRequestApprovals");

            migrationBuilder.RenameIndex(
                name: "IX_LeaveRequestApproval_UserId",
                table: "LeaveRequestApprovals",
                newName: "IX_LeaveRequestApprovals_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_LeaveRequestApproval_RoleId",
                table: "LeaveRequestApprovals",
                newName: "IX_LeaveRequestApprovals_RoleId");

            migrationBuilder.RenameIndex(
                name: "IX_LeaveRequestApproval_LeaveRequestId",
                table: "LeaveRequestApprovals",
                newName: "IX_LeaveRequestApprovals_LeaveRequestId");

            migrationBuilder.RenameIndex(
                name: "IX_LeaveRequestApproval_ApprovedById",
                table: "LeaveRequestApprovals",
                newName: "IX_LeaveRequestApprovals_ApprovedById");

            migrationBuilder.RenameIndex(
                name: "IX_LeaveRequestApproval_ApprovalId",
                table: "LeaveRequestApprovals",
                newName: "IX_LeaveRequestApprovals_ApprovalId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LeaveRequestApprovals",
                table: "LeaveRequestApprovals",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveRequestApprovals_Approvals_ApprovalId",
                table: "LeaveRequestApprovals",
                column: "ApprovalId",
                principalTable: "Approvals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveRequestApprovals_LeaveRequests_LeaveRequestId",
                table: "LeaveRequestApprovals",
                column: "LeaveRequestId",
                principalTable: "LeaveRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveRequestApprovals_roles_RoleId",
                table: "LeaveRequestApprovals",
                column: "RoleId",
                principalTable: "roles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveRequestApprovals_users_ApprovedById",
                table: "LeaveRequestApprovals",
                column: "ApprovedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveRequestApprovals_users_UserId",
                table: "LeaveRequestApprovals",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LeaveRequestApprovals_Approvals_ApprovalId",
                table: "LeaveRequestApprovals");

            migrationBuilder.DropForeignKey(
                name: "FK_LeaveRequestApprovals_LeaveRequests_LeaveRequestId",
                table: "LeaveRequestApprovals");

            migrationBuilder.DropForeignKey(
                name: "FK_LeaveRequestApprovals_roles_RoleId",
                table: "LeaveRequestApprovals");

            migrationBuilder.DropForeignKey(
                name: "FK_LeaveRequestApprovals_users_ApprovedById",
                table: "LeaveRequestApprovals");

            migrationBuilder.DropForeignKey(
                name: "FK_LeaveRequestApprovals_users_UserId",
                table: "LeaveRequestApprovals");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LeaveRequestApprovals",
                table: "LeaveRequestApprovals");

            migrationBuilder.RenameTable(
                name: "LeaveRequestApprovals",
                newName: "LeaveRequestApproval");

            migrationBuilder.RenameIndex(
                name: "IX_LeaveRequestApprovals_UserId",
                table: "LeaveRequestApproval",
                newName: "IX_LeaveRequestApproval_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_LeaveRequestApprovals_RoleId",
                table: "LeaveRequestApproval",
                newName: "IX_LeaveRequestApproval_RoleId");

            migrationBuilder.RenameIndex(
                name: "IX_LeaveRequestApprovals_LeaveRequestId",
                table: "LeaveRequestApproval",
                newName: "IX_LeaveRequestApproval_LeaveRequestId");

            migrationBuilder.RenameIndex(
                name: "IX_LeaveRequestApprovals_ApprovedById",
                table: "LeaveRequestApproval",
                newName: "IX_LeaveRequestApproval_ApprovedById");

            migrationBuilder.RenameIndex(
                name: "IX_LeaveRequestApprovals_ApprovalId",
                table: "LeaveRequestApproval",
                newName: "IX_LeaveRequestApproval_ApprovalId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LeaveRequestApproval",
                table: "LeaveRequestApproval",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveRequestApproval_Approvals_ApprovalId",
                table: "LeaveRequestApproval",
                column: "ApprovalId",
                principalTable: "Approvals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveRequestApproval_LeaveRequests_LeaveRequestId",
                table: "LeaveRequestApproval",
                column: "LeaveRequestId",
                principalTable: "LeaveRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveRequestApproval_roles_RoleId",
                table: "LeaveRequestApproval",
                column: "RoleId",
                principalTable: "roles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveRequestApproval_users_ApprovedById",
                table: "LeaveRequestApproval",
                column: "ApprovedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveRequestApproval_users_UserId",
                table: "LeaveRequestApproval",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id");
        }
    }
}
