using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUoMAndProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductionOrderInvoiceItems");

            migrationBuilder.DropTable(
                name: "ProductionOrderInvoices");

            migrationBuilder.DropColumn(
                name: "CustomerPoNumber",
                table: "Invoices");

            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "UnitOfMeasures",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "UnitOfMeasures",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "ExpectedYield",
                table: "Products",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "Approved",
                table: "ProductionOrders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveredAt",
                table: "ProductionOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "ProductionOrders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "CustomerId",
                table: "Invoices",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "ProductionOrderApprovals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionOrderId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_ProductionOrderApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionOrderApprovals_Approvals_ApprovalId",
                        column: x => x.ApprovalId,
                        principalTable: "Approvals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionOrderApprovals_ProductionOrders_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalTable: "ProductionOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionOrderApprovals_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionOrderApprovals_users_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionOrderApprovals_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_CustomerId",
                table: "Invoices",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrderApprovals_ApprovalId",
                table: "ProductionOrderApprovals",
                column: "ApprovalId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrderApprovals_ApprovedById",
                table: "ProductionOrderApprovals",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrderApprovals_ProductionOrderId",
                table: "ProductionOrderApprovals",
                column: "ProductionOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrderApprovals_RoleId",
                table: "ProductionOrderApprovals",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrderApprovals_UserId",
                table: "ProductionOrderApprovals",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Customers_CustomerId",
                table: "Invoices",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Customers_CustomerId",
                table: "Invoices");

            migrationBuilder.DropTable(
                name: "ProductionOrderApprovals");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_CustomerId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "UnitOfMeasures");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "UnitOfMeasures");

            migrationBuilder.DropColumn(
                name: "ExpectedYield",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Approved",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "DeliveredAt",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "Invoices");

            migrationBuilder.AddColumn<string>(
                name: "CustomerPoNumber",
                table: "Invoices",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProductionOrderInvoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductionOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionOrderInvoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionOrderInvoices_ProductionOrders_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalTable: "ProductionOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionOrderInvoices_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionOrderInvoices_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionOrderInvoices_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProductionOrderInvoiceItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionOrderInvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionOrderInvoiceItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionOrderInvoiceItems_ProductionOrderInvoices_Product~",
                        column: x => x.ProductionOrderInvoiceId,
                        principalTable: "ProductionOrderInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionOrderInvoiceItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionOrderInvoiceItems_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionOrderInvoiceItems_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionOrderInvoiceItems_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrderInvoiceItems_CreatedById",
                table: "ProductionOrderInvoiceItems",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrderInvoiceItems_LastDeletedById",
                table: "ProductionOrderInvoiceItems",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrderInvoiceItems_LastUpdatedById",
                table: "ProductionOrderInvoiceItems",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrderInvoiceItems_ProductId",
                table: "ProductionOrderInvoiceItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrderInvoiceItems_ProductionOrderInvoiceId",
                table: "ProductionOrderInvoiceItems",
                column: "ProductionOrderInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrderInvoices_CreatedById",
                table: "ProductionOrderInvoices",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrderInvoices_LastDeletedById",
                table: "ProductionOrderInvoices",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrderInvoices_LastUpdatedById",
                table: "ProductionOrderInvoices",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrderInvoices_ProductionOrderId",
                table: "ProductionOrderInvoices",
                column: "ProductionOrderId");
        }
    }
}
