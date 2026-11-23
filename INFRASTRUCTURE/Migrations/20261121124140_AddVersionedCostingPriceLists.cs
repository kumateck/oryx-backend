using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddVersionedCostingPriceLists : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PriceCurrencyId",
                table: "ProformaInvoiceProducts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PriceListRevisionId",
                table: "ProformaInvoiceProducts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProductPackingId",
                table: "ProformaInvoiceProducts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitPrice",
                table: "ProformaInvoiceProducts",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PriceCurrencyId",
                table: "ProductionOrderProducts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PriceListRevisionId",
                table: "ProductionOrderProducts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PriceListRevisionId",
                table: "CustomerQuotationItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CostingPriceLists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Division = table.Column<int>(type: "integer", nullable: false),
                    DocumentNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RevisionNumber = table.Column<int>(type: "integer", nullable: false),
                    PreviousRevisionId = table.Column<Guid>(type: "uuid", nullable: true),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevisionReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostingPriceLists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CostingPriceLists_CostingPriceLists_PreviousRevisionId",
                        column: x => x.PreviousRevisionId,
                        principalTable: "CostingPriceLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CostingPriceLists_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CostingPriceLists_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CostingPriceLists_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CostingPriceListApprovals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CostingPriceListId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_CostingPriceListApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CostingPriceListApprovals_Approvals_ApprovalId",
                        column: x => x.ApprovalId,
                        principalTable: "Approvals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CostingPriceListApprovals_CostingPriceLists_CostingPriceLis~",
                        column: x => x.CostingPriceListId,
                        principalTable: "CostingPriceLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CostingPriceListApprovals_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CostingPriceListApprovals_users_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CostingPriceListApprovals_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CostingPriceListLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CostingPriceListId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductPackingId = table.Column<Guid>(type: "uuid", nullable: false),
                    PackingStyle = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductCode = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ProductName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Price = table.Column<decimal>(type: "numeric(29,10)", precision: 29, scale: 10, nullable: true),
                    PriceUoM = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CurrencyId = table.Column<Guid>(type: "uuid", nullable: true),
                    CurrencyName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CurrencySymbol = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostingPriceListLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CostingPriceListLines_CostingPriceLists_CostingPriceListId",
                        column: x => x.CostingPriceListId,
                        principalTable: "CostingPriceLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CostingPriceListLines_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CostingPriceListLines_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CostingPriceListLines_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CostingPriceListApprovals_ApprovalId",
                table: "CostingPriceListApprovals",
                column: "ApprovalId");

            migrationBuilder.CreateIndex(
                name: "IX_CostingPriceListApprovals_ApprovedById",
                table: "CostingPriceListApprovals",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_CostingPriceListApprovals_CostingPriceListId",
                table: "CostingPriceListApprovals",
                column: "CostingPriceListId");

            migrationBuilder.CreateIndex(
                name: "IX_CostingPriceListApprovals_RoleId",
                table: "CostingPriceListApprovals",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_CostingPriceListApprovals_UserId",
                table: "CostingPriceListApprovals",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CostingPriceListLines_CostingPriceListId_ProductId_ProductP~",
                table: "CostingPriceListLines",
                columns: new[] { "CostingPriceListId", "ProductId", "ProductPackingId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CostingPriceListLines_CreatedById",
                table: "CostingPriceListLines",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_CostingPriceListLines_LastDeletedById",
                table: "CostingPriceListLines",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_CostingPriceListLines_LastUpdatedById",
                table: "CostingPriceListLines",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_CostingPriceListLines_ProductId",
                table: "CostingPriceListLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_CostingPriceLists_CreatedById",
                table: "CostingPriceLists",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_CostingPriceLists_Division_RevisionNumber",
                table: "CostingPriceLists",
                columns: new[] { "Division", "RevisionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CostingPriceLists_Division_Status_EffectiveFrom",
                table: "CostingPriceLists",
                columns: new[] { "Division", "Status", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_CostingPriceLists_LastDeletedById",
                table: "CostingPriceLists",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_CostingPriceLists_LastUpdatedById",
                table: "CostingPriceLists",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_CostingPriceLists_OneOpenPerDivision",
                table: "CostingPriceLists",
                column: "Division",
                unique: true,
                filter: "\"DeletedAt\" IS NULL AND \"Status\" IN (0, 4)");

            migrationBuilder.CreateIndex(
                name: "IX_CostingPriceLists_PreviousRevisionId",
                table: "CostingPriceLists",
                column: "PreviousRevisionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CostingPriceListApprovals");

            migrationBuilder.DropTable(
                name: "CostingPriceListLines");

            migrationBuilder.DropTable(
                name: "CostingPriceLists");

            migrationBuilder.DropColumn(
                name: "PriceCurrencyId",
                table: "ProformaInvoiceProducts");

            migrationBuilder.DropColumn(
                name: "PriceListRevisionId",
                table: "ProformaInvoiceProducts");

            migrationBuilder.DropColumn(
                name: "ProductPackingId",
                table: "ProformaInvoiceProducts");

            migrationBuilder.DropColumn(
                name: "UnitPrice",
                table: "ProformaInvoiceProducts");

            migrationBuilder.DropColumn(
                name: "PriceCurrencyId",
                table: "ProductionOrderProducts");

            migrationBuilder.DropColumn(
                name: "PriceListRevisionId",
                table: "ProductionOrderProducts");

            migrationBuilder.DropColumn(
                name: "PriceListRevisionId",
                table: "CustomerQuotationItems");
        }
    }
}
