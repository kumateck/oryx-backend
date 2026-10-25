using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class StockDiscrepancyApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StockDiscrepancies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShelfMaterialBatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    SystemQuantitySnapshot = table.Column<decimal>(type: "numeric", nullable: false),
                    PhysicalQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    ReasonCode = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Approved = table.Column<bool>(type: "boolean", nullable: false),
                    Rejected = table.Column<bool>(type: "boolean", nullable: false),
                    ReconciledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReconciledById = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockDiscrepancies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockDiscrepancies_ShelfMaterialBatches_ShelfMaterialBatchId",
                        column: x => x.ShelfMaterialBatchId,
                        principalTable: "ShelfMaterialBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockDiscrepancies_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockDiscrepancies_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockDiscrepancies_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StockDiscrepancyApprovals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StockDiscrepancyId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_StockDiscrepancyApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockDiscrepancyApprovals_Approvals_ApprovalId",
                        column: x => x.ApprovalId,
                        principalTable: "Approvals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockDiscrepancyApprovals_StockDiscrepancies_StockDiscrepan~",
                        column: x => x.StockDiscrepancyId,
                        principalTable: "StockDiscrepancies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockDiscrepancyApprovals_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockDiscrepancyApprovals_users_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockDiscrepancyApprovals_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockDiscrepancies_CreatedById",
                table: "StockDiscrepancies",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_StockDiscrepancies_LastDeletedById",
                table: "StockDiscrepancies",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_StockDiscrepancies_LastUpdatedById",
                table: "StockDiscrepancies",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_StockDiscrepancies_ShelfMaterialBatchId",
                table: "StockDiscrepancies",
                column: "ShelfMaterialBatchId",
                unique: true,
                filter: "\"Rejected\" = false AND \"ReconciledAt\" IS NULL AND \"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StockDiscrepancyApprovals_ApprovalId",
                table: "StockDiscrepancyApprovals",
                column: "ApprovalId");

            migrationBuilder.CreateIndex(
                name: "IX_StockDiscrepancyApprovals_StageUnique",
                table: "StockDiscrepancyApprovals",
                columns: new[] { "ApprovalId", "StockDiscrepancyId", "Order", "UserId", "RoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockDiscrepancyApprovals_ApprovedById",
                table: "StockDiscrepancyApprovals",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_StockDiscrepancyApprovals_RoleId",
                table: "StockDiscrepancyApprovals",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_StockDiscrepancyApprovals_StockDiscrepancyId",
                table: "StockDiscrepancyApprovals",
                column: "StockDiscrepancyId");

            migrationBuilder.CreateIndex(
                name: "IX_StockDiscrepancyApprovals_UserId",
                table: "StockDiscrepancyApprovals",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockDiscrepancyApprovals");

            migrationBuilder.DropTable(
                name: "StockDiscrepancies");
        }
    }
}
