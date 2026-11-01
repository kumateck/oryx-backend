using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddStockRequisitionIssueCheck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StockRequisitionIssues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequisitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    IssuedById = table.Column<Guid>(type: "uuid", nullable: false),
                    IssuedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Approved = table.Column<bool>(type: "boolean", nullable: false),
                    Rejected = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockRequisitionIssues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockRequisitionIssues_Requisitions_RequisitionId",
                        column: x => x.RequisitionId,
                        principalTable: "Requisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockRequisitionIssues_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockRequisitionIssues_users_IssuedById",
                        column: x => x.IssuedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockRequisitionIssues_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockRequisitionIssues_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StockRequisitionIssueApprovals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StockRequisitionIssueId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_StockRequisitionIssueApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockRequisitionIssueApprovals_Approvals_ApprovalId",
                        column: x => x.ApprovalId,
                        principalTable: "Approvals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockRequisitionIssueApprovals_StockRequisitionIssues_Stock~",
                        column: x => x.StockRequisitionIssueId,
                        principalTable: "StockRequisitionIssues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockRequisitionIssueApprovals_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockRequisitionIssueApprovals_users_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockRequisitionIssueApprovals_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockRequisitionIssueApprovals_ApprovedById",
                table: "StockRequisitionIssueApprovals",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_StockRequisitionIssueApprovals_RoleId",
                table: "StockRequisitionIssueApprovals",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_StockRequisitionIssueApprovals_StageUnique",
                table: "StockRequisitionIssueApprovals",
                columns: new[] { "ApprovalId", "StockRequisitionIssueId", "Order", "UserId", "RoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockRequisitionIssueApprovals_StockRequisitionIssueId",
                table: "StockRequisitionIssueApprovals",
                column: "StockRequisitionIssueId");

            migrationBuilder.CreateIndex(
                name: "IX_StockRequisitionIssueApprovals_UserId",
                table: "StockRequisitionIssueApprovals",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_StockRequisitionIssues_CreatedById",
                table: "StockRequisitionIssues",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_StockRequisitionIssues_IssuedById",
                table: "StockRequisitionIssues",
                column: "IssuedById");

            migrationBuilder.CreateIndex(
                name: "IX_StockRequisitionIssues_LastDeletedById",
                table: "StockRequisitionIssues",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_StockRequisitionIssues_LastUpdatedById",
                table: "StockRequisitionIssues",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_StockRequisitionIssues_RequisitionId",
                table: "StockRequisitionIssues",
                column: "RequisitionId",
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            // Requisitions issued before this change: the issuer is whoever wrote the issue's bin cards.
            // They have no issue check, so their vouchers show it as not assigned.
            // RequisitionType 0 = Stock, RequestStatus 3 = Completed.
            migrationBuilder.Sql("""
                INSERT INTO "StockRequisitionIssues" ("Id", "RequisitionId", "IssuedById", "IssuedAt", "Approved", "Rejected", "CreatedAt", "CreatedById")
                SELECT gen_random_uuid(), issued."RequisitionId", issued."CreatedById", issued."IssuedAt", FALSE, FALSE, issued."IssuedAt", issued."CreatedById"
                FROM (
                    SELECT DISTINCT ON (b."RequisitionId") b."RequisitionId", b."CreatedById", b."CreatedAt" AS "IssuedAt"
                    FROM "BinCardInformation" AS b
                    JOIN "Requisitions" AS r ON r."Id" = b."RequisitionId"
                    JOIN "users" AS u ON u."Id" = b."CreatedById"
                    WHERE r."RequisitionType" = 0 AND r."Status" = 3 AND r."DeletedAt" IS NULL
                      AND b."QuantityIssued" > 0 AND b."CreatedById" IS NOT NULL AND b."DeletedAt" IS NULL
                    ORDER BY b."RequisitionId", b."CreatedAt"
                ) AS issued
                WHERE NOT EXISTS (SELECT 1 FROM "StockRequisitionIssues" AS existing WHERE existing."RequisitionId" = issued."RequisitionId");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockRequisitionIssueApprovals");

            migrationBuilder.DropTable(
                name: "StockRequisitionIssues");
        }
    }
}
