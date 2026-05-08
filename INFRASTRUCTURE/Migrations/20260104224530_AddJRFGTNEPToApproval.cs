using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddJRFGTNEPToApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Approved",
                table: "ProductionExtraPackings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Approved",
                table: "JobRequests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Approved",
                table: "FinishedGoodsTransferNotes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "FinishedGoodsTransferNoteApprovals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FinishedGoodsTransferNoteId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_FinishedGoodsTransferNoteApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinishedGoodsTransferNoteApprovals_Approvals_ApprovalId",
                        column: x => x.ApprovalId,
                        principalTable: "Approvals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FinishedGoodsTransferNoteApprovals_FinishedGoodsTransferNot~",
                        column: x => x.FinishedGoodsTransferNoteId,
                        principalTable: "FinishedGoodsTransferNotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FinishedGoodsTransferNoteApprovals_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FinishedGoodsTransferNoteApprovals_users_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FinishedGoodsTransferNoteApprovals_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "JobRequestApprovals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobRequestId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_JobRequestApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobRequestApprovals_Approvals_ApprovalId",
                        column: x => x.ApprovalId,
                        principalTable: "Approvals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JobRequestApprovals_JobRequests_JobRequestId",
                        column: x => x.JobRequestId,
                        principalTable: "JobRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JobRequestApprovals_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_JobRequestApprovals_users_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_JobRequestApprovals_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProductionExtraPackingApprovals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionExtraPackingId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_ProductionExtraPackingApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionExtraPackingApprovals_Approvals_ApprovalId",
                        column: x => x.ApprovalId,
                        principalTable: "Approvals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionExtraPackingApprovals_ProductionExtraPackings_Pro~",
                        column: x => x.ProductionExtraPackingId,
                        principalTable: "ProductionExtraPackings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionExtraPackingApprovals_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionExtraPackingApprovals_users_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionExtraPackingApprovals_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_FinishedGoodsTransferNoteApprovals_ApprovalId",
                table: "FinishedGoodsTransferNoteApprovals",
                column: "ApprovalId");

            migrationBuilder.CreateIndex(
                name: "IX_FinishedGoodsTransferNoteApprovals_ApprovedById",
                table: "FinishedGoodsTransferNoteApprovals",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_FinishedGoodsTransferNoteApprovals_FinishedGoodsTransferNot~",
                table: "FinishedGoodsTransferNoteApprovals",
                column: "FinishedGoodsTransferNoteId");

            migrationBuilder.CreateIndex(
                name: "IX_FinishedGoodsTransferNoteApprovals_RoleId",
                table: "FinishedGoodsTransferNoteApprovals",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_FinishedGoodsTransferNoteApprovals_UserId",
                table: "FinishedGoodsTransferNoteApprovals",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_JobRequestApprovals_ApprovalId",
                table: "JobRequestApprovals",
                column: "ApprovalId");

            migrationBuilder.CreateIndex(
                name: "IX_JobRequestApprovals_ApprovedById",
                table: "JobRequestApprovals",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_JobRequestApprovals_JobRequestId",
                table: "JobRequestApprovals",
                column: "JobRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_JobRequestApprovals_RoleId",
                table: "JobRequestApprovals",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_JobRequestApprovals_UserId",
                table: "JobRequestApprovals",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionExtraPackingApprovals_ApprovalId",
                table: "ProductionExtraPackingApprovals",
                column: "ApprovalId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionExtraPackingApprovals_ApprovedById",
                table: "ProductionExtraPackingApprovals",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionExtraPackingApprovals_ProductionExtraPackingId",
                table: "ProductionExtraPackingApprovals",
                column: "ProductionExtraPackingId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionExtraPackingApprovals_RoleId",
                table: "ProductionExtraPackingApprovals",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionExtraPackingApprovals_UserId",
                table: "ProductionExtraPackingApprovals",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FinishedGoodsTransferNoteApprovals");

            migrationBuilder.DropTable(
                name: "JobRequestApprovals");

            migrationBuilder.DropTable(
                name: "ProductionExtraPackingApprovals");

            migrationBuilder.DropColumn(
                name: "Approved",
                table: "ProductionExtraPackings");

            migrationBuilder.DropColumn(
                name: "Approved",
                table: "JobRequests");

            migrationBuilder.DropColumn(
                name: "Approved",
                table: "FinishedGoodsTransferNotes");
        }
    }
}
