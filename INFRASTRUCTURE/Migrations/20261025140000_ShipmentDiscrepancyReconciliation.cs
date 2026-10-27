using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class ShipmentDiscrepancyReconciliation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FulfillmentRequestKey",
                table: "ShipmentInvoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Kind",
                table: "ShipmentInvoices",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "FulfillsDiscrepancyItemId",
                table: "ShipmentInvoiceItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DeclaredQuantitySnapshot",
                table: "ShipmentDiscrepancyItem",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "ShipmentInvoiceItemId",
                table: "ShipmentDiscrepancyItem",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Approved",
                table: "ShipmentDiscrepancies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Rejected",
                table: "ShipmentDiscrepancies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ShipmentDiscrepancyApprovals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShipmentDiscrepancyId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_ShipmentDiscrepancyApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShipmentDiscrepancyApprovals_Approvals_ApprovalId",
                        column: x => x.ApprovalId,
                        principalTable: "Approvals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShipmentDiscrepancyApprovals_ShipmentDiscrepancies_Shipment~",
                        column: x => x.ShipmentDiscrepancyId,
                        principalTable: "ShipmentDiscrepancies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShipmentDiscrepancyApprovals_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ShipmentDiscrepancyApprovals_users_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ShipmentDiscrepancyApprovals_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ShipmentShortageReceipts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShipmentInvoiceItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChecklistId = table.Column<Guid>(type: "uuid", nullable: false),
                    PhysicalQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipmentShortageReceipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShipmentShortageReceipts_Checklists_ChecklistId",
                        column: x => x.ChecklistId,
                        principalTable: "Checklists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShipmentShortageReceipts_ShipmentInvoiceItems_ShipmentInvoi~",
                        column: x => x.ShipmentInvoiceItemId,
                        principalTable: "ShipmentInvoiceItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShipmentShortageReceipts_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ShipmentShortageReceipts_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ShipmentShortageReceipts_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentInvoices_FulfillmentRequestKey",
                table: "ShipmentInvoices",
                column: "FulfillmentRequestKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentInvoiceItems_FulfillsDiscrepancyItemId",
                table: "ShipmentInvoiceItems",
                column: "FulfillsDiscrepancyItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentDiscrepancyItem_ShipmentInvoiceItemId",
                table: "ShipmentDiscrepancyItem",
                column: "ShipmentInvoiceItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentDiscrepancyApprovals_ApprovalId_ShipmentDiscrepancy~",
                table: "ShipmentDiscrepancyApprovals",
                columns: new[] { "ApprovalId", "ShipmentDiscrepancyId", "Order", "UserId", "RoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentDiscrepancyApprovals_ApprovedById",
                table: "ShipmentDiscrepancyApprovals",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentDiscrepancyApprovals_RoleId",
                table: "ShipmentDiscrepancyApprovals",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentDiscrepancyApprovals_ShipmentDiscrepancyId",
                table: "ShipmentDiscrepancyApprovals",
                column: "ShipmentDiscrepancyId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentDiscrepancyApprovals_UserId",
                table: "ShipmentDiscrepancyApprovals",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentShortageReceipts_ChecklistId",
                table: "ShipmentShortageReceipts",
                column: "ChecklistId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentShortageReceipts_CreatedById",
                table: "ShipmentShortageReceipts",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentShortageReceipts_LastDeletedById",
                table: "ShipmentShortageReceipts",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentShortageReceipts_LastUpdatedById",
                table: "ShipmentShortageReceipts",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentShortageReceipts_ShipmentInvoiceItemId_ChecklistId",
                table: "ShipmentShortageReceipts",
                columns: new[] { "ShipmentInvoiceItemId", "ChecklistId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ShipmentDiscrepancyItem_ShipmentInvoiceItems_ShipmentInvoic~",
                table: "ShipmentDiscrepancyItem",
                column: "ShipmentInvoiceItemId",
                principalTable: "ShipmentInvoiceItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ShipmentInvoiceItems_ShipmentDiscrepancyItem_FulfillsDiscre~",
                table: "ShipmentInvoiceItems",
                column: "FulfillsDiscrepancyItemId",
                principalTable: "ShipmentDiscrepancyItem",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShipmentDiscrepancyItem_ShipmentInvoiceItems_ShipmentInvoic~",
                table: "ShipmentDiscrepancyItem");

            migrationBuilder.DropForeignKey(
                name: "FK_ShipmentInvoiceItems_ShipmentDiscrepancyItem_FulfillsDiscre~",
                table: "ShipmentInvoiceItems");

            migrationBuilder.DropTable(
                name: "ShipmentDiscrepancyApprovals");

            migrationBuilder.DropTable(
                name: "ShipmentShortageReceipts");

            migrationBuilder.DropIndex(
                name: "IX_ShipmentInvoices_FulfillmentRequestKey",
                table: "ShipmentInvoices");

            migrationBuilder.DropIndex(
                name: "IX_ShipmentInvoiceItems_FulfillsDiscrepancyItemId",
                table: "ShipmentInvoiceItems");

            migrationBuilder.DropIndex(
                name: "IX_ShipmentDiscrepancyItem_ShipmentInvoiceItemId",
                table: "ShipmentDiscrepancyItem");

            migrationBuilder.DropColumn(
                name: "FulfillmentRequestKey",
                table: "ShipmentInvoices");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "ShipmentInvoices");

            migrationBuilder.DropColumn(
                name: "FulfillsDiscrepancyItemId",
                table: "ShipmentInvoiceItems");

            migrationBuilder.DropColumn(
                name: "DeclaredQuantitySnapshot",
                table: "ShipmentDiscrepancyItem");

            migrationBuilder.DropColumn(
                name: "ShipmentInvoiceItemId",
                table: "ShipmentDiscrepancyItem");

            migrationBuilder.DropColumn(
                name: "Approved",
                table: "ShipmentDiscrepancies");

            migrationBuilder.DropColumn(
                name: "Rejected",
                table: "ShipmentDiscrepancies");
        }
    }
}
