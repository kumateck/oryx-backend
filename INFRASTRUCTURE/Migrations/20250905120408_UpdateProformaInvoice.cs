using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class UpdateProformaInvoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProformaInvoices_ProductionOrders_ProductionOrderId",
                table: "ProformaInvoices");

            migrationBuilder.RenameColumn(
                name: "ProductionOrderId",
                table: "ProformaInvoices",
                newName: "AllocateProductionOrderId");

            migrationBuilder.RenameIndex(
                name: "IX_ProformaInvoices_ProductionOrderId",
                table: "ProformaInvoices",
                newName: "IX_ProformaInvoices_AllocateProductionOrderId");

            migrationBuilder.AddColumn<bool>(
                name: "Approved",
                table: "ProformaInvoices",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "ProformaInvoices",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "WarehouseId",
                table: "BinCardInformation",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProformaInvoiceApprovals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProformaInvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_ProformaInvoiceApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProformaInvoiceApprovals_Approvals_ApprovalId",
                        column: x => x.ApprovalId,
                        principalTable: "Approvals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProformaInvoiceApprovals_ProformaInvoices_ProformaInvoiceId",
                        column: x => x.ProformaInvoiceId,
                        principalTable: "ProformaInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProformaInvoiceApprovals_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProformaInvoiceApprovals_users_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProformaInvoiceApprovals_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_BinCardInformation_WarehouseId",
                table: "BinCardInformation",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_ProformaInvoiceApprovals_ApprovalId",
                table: "ProformaInvoiceApprovals",
                column: "ApprovalId");

            migrationBuilder.CreateIndex(
                name: "IX_ProformaInvoiceApprovals_ApprovedById",
                table: "ProformaInvoiceApprovals",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProformaInvoiceApprovals_ProformaInvoiceId",
                table: "ProformaInvoiceApprovals",
                column: "ProformaInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ProformaInvoiceApprovals_RoleId",
                table: "ProformaInvoiceApprovals",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_ProformaInvoiceApprovals_UserId",
                table: "ProformaInvoiceApprovals",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_BinCardInformation_Warehouses_WarehouseId",
                table: "BinCardInformation",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProformaInvoices_AllocateProductionOrders_AllocateProductio~",
                table: "ProformaInvoices",
                column: "AllocateProductionOrderId",
                principalTable: "AllocateProductionOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BinCardInformation_Warehouses_WarehouseId",
                table: "BinCardInformation");

            migrationBuilder.DropForeignKey(
                name: "FK_ProformaInvoices_AllocateProductionOrders_AllocateProductio~",
                table: "ProformaInvoices");

            migrationBuilder.DropTable(
                name: "ProformaInvoiceApprovals");

            migrationBuilder.DropIndex(
                name: "IX_BinCardInformation_WarehouseId",
                table: "BinCardInformation");

            migrationBuilder.DropColumn(
                name: "Approved",
                table: "ProformaInvoices");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "ProformaInvoices");

            migrationBuilder.DropColumn(
                name: "WarehouseId",
                table: "BinCardInformation");

            migrationBuilder.RenameColumn(
                name: "AllocateProductionOrderId",
                table: "ProformaInvoices",
                newName: "ProductionOrderId");

            migrationBuilder.RenameIndex(
                name: "IX_ProformaInvoices_AllocateProductionOrderId",
                table: "ProformaInvoices",
                newName: "IX_ProformaInvoices_ProductionOrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProformaInvoices_ProductionOrders_ProductionOrderId",
                table: "ProformaInvoices",
                column: "ProductionOrderId",
                principalTable: "ProductionOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
