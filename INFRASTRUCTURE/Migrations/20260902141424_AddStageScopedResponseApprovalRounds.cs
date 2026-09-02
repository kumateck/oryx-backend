using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddStageScopedResponseApprovalRounds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ResponseApprovals_ApprovalId_ResponseId_Order_UserId_RoleId",
                table: "ResponseApprovals");

            migrationBuilder.AddColumn<int>(
                name: "ApprovalRound",
                table: "ResponseApprovals",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "Complies",
                table: "FormResponses",
                type: "boolean",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResponseApprovals_ApprovalId_ResponseId_ApprovalRound_Order~",
                table: "ResponseApprovals",
                columns: new[] { "ApprovalId", "ResponseId", "ApprovalRound", "Order", "UserId", "RoleId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ResponseApprovals_ApprovalId_ResponseId_ApprovalRound_Order~",
                table: "ResponseApprovals");

            migrationBuilder.DropColumn(
                name: "ApprovalRound",
                table: "ResponseApprovals");

            migrationBuilder.DropColumn(
                name: "Complies",
                table: "FormResponses");

            migrationBuilder.CreateIndex(
                name: "IX_ResponseApprovals_ApprovalId_ResponseId_Order_UserId_RoleId",
                table: "ResponseApprovals",
                columns: new[] { "ApprovalId", "ResponseId", "Order", "UserId", "RoleId" },
                unique: true);
        }
    }
}
