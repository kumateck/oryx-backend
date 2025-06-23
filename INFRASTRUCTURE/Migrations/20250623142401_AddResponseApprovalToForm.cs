using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddResponseApprovalToForm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Approved",
                table: "Responses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "MaterialAnalyticalRawDataId",
                table: "Responses",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ResponseApprovals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ResponseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovalId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: true),
                    Required = table.Column<bool>(type: "boolean", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    StageStartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ApprovalTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedById = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActivatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Comments = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResponseApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResponseApprovals_Approvals_ApprovalId",
                        column: x => x.ApprovalId,
                        principalTable: "Approvals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ResponseApprovals_Responses_ResponseId",
                        column: x => x.ResponseId,
                        principalTable: "Responses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ResponseApprovals_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ResponseApprovals_users_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ResponseApprovals_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Responses_MaterialAnalyticalRawDataId",
                table: "Responses",
                column: "MaterialAnalyticalRawDataId");

            migrationBuilder.CreateIndex(
                name: "IX_ResponseApprovals_ApprovalId",
                table: "ResponseApprovals",
                column: "ApprovalId");

            migrationBuilder.CreateIndex(
                name: "IX_ResponseApprovals_ApprovedById",
                table: "ResponseApprovals",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_ResponseApprovals_ResponseId",
                table: "ResponseApprovals",
                column: "ResponseId");

            migrationBuilder.CreateIndex(
                name: "IX_ResponseApprovals_RoleId",
                table: "ResponseApprovals",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_ResponseApprovals_UserId",
                table: "ResponseApprovals",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Responses_MaterialAnalyticalRawData_MaterialAnalyticalRawDa~",
                table: "Responses",
                column: "MaterialAnalyticalRawDataId",
                principalTable: "MaterialAnalyticalRawData",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Responses_MaterialAnalyticalRawData_MaterialAnalyticalRawDa~",
                table: "Responses");

            migrationBuilder.DropTable(
                name: "ResponseApprovals");

            migrationBuilder.DropIndex(
                name: "IX_Responses_MaterialAnalyticalRawDataId",
                table: "Responses");

            migrationBuilder.DropColumn(
                name: "Approved",
                table: "Responses");

            migrationBuilder.DropColumn(
                name: "MaterialAnalyticalRawDataId",
                table: "Responses");
        }
    }
}
