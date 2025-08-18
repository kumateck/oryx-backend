using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddOvertimeRequestApprovals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ApprovalStatus",
                table: "OvertimeRequests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "Approved",
                table: "OvertimeRequests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "OvertimeRequestApprovals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OvertimeRequestId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_OvertimeRequestApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OvertimeRequestApprovals_Approvals_ApprovalId",
                        column: x => x.ApprovalId,
                        principalTable: "Approvals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OvertimeRequestApprovals_OvertimeRequests_OvertimeRequestId",
                        column: x => x.OvertimeRequestId,
                        principalTable: "OvertimeRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OvertimeRequestApprovals_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OvertimeRequestApprovals_users_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OvertimeRequestApprovals_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StockEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    MemoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockEntries_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockEntries_Memos_MemoId",
                        column: x => x.MemoId,
                        principalTable: "Memos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockEntries_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockEntries_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockEntries_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_OvertimeRequestApprovals_ApprovalId",
                table: "OvertimeRequestApprovals",
                column: "ApprovalId");

            migrationBuilder.CreateIndex(
                name: "IX_OvertimeRequestApprovals_ApprovedById",
                table: "OvertimeRequestApprovals",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_OvertimeRequestApprovals_OvertimeRequestId",
                table: "OvertimeRequestApprovals",
                column: "OvertimeRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_OvertimeRequestApprovals_RoleId",
                table: "OvertimeRequestApprovals",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_OvertimeRequestApprovals_UserId",
                table: "OvertimeRequestApprovals",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_StockEntries_CreatedById",
                table: "StockEntries",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_StockEntries_ItemId",
                table: "StockEntries",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_StockEntries_LastDeletedById",
                table: "StockEntries",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_StockEntries_LastUpdatedById",
                table: "StockEntries",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_StockEntries_MemoId",
                table: "StockEntries",
                column: "MemoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OvertimeRequestApprovals");

            migrationBuilder.DropTable(
                name: "StockEntries");

            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "OvertimeRequests");

            migrationBuilder.DropColumn(
                name: "Approved",
                table: "OvertimeRequests");
        }
    }
}
