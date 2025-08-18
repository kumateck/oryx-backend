using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "NonProductionSupplierId",
                table: "Inventories",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "NonProductionSuppliers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Address = table.Column<string>(type: "text", nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    CountryId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrencyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NonProductionSuppliers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NonProductionSuppliers_Countries_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Countries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NonProductionSuppliers_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NonProductionSuppliers_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_NonProductionSuppliers_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_NonProductionSuppliers_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProformaInvoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProformaInvoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProformaInvoices_ProductionOrders_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalTable: "ProductionOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProformaInvoices_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProformaInvoices_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProformaInvoices_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Invoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProformaInvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerPoNumber = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Invoices_ProformaInvoices_ProformaInvoiceId",
                        column: x => x.ProformaInvoiceId,
                        principalTable: "ProformaInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Invoices_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Invoices_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Invoices_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProformaInvoiceProducts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProformaInvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProformaInvoiceProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProformaInvoiceProducts_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProformaInvoiceProducts_ProformaInvoices_ProformaInvoiceId",
                        column: x => x.ProformaInvoiceId,
                        principalTable: "ProformaInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProformaInvoiceProducts_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProformaInvoiceProducts_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProformaInvoiceProducts_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_NonProductionSupplierId",
                table: "Inventories",
                column: "NonProductionSupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_CreatedById",
                table: "Invoices",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_LastDeletedById",
                table: "Invoices",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_LastUpdatedById",
                table: "Invoices",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_ProformaInvoiceId",
                table: "Invoices",
                column: "ProformaInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_NonProductionSuppliers_CountryId",
                table: "NonProductionSuppliers",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_NonProductionSuppliers_CreatedById",
                table: "NonProductionSuppliers",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_NonProductionSuppliers_CurrencyId",
                table: "NonProductionSuppliers",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_NonProductionSuppliers_LastDeletedById",
                table: "NonProductionSuppliers",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_NonProductionSuppliers_LastUpdatedById",
                table: "NonProductionSuppliers",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProformaInvoiceProducts_CreatedById",
                table: "ProformaInvoiceProducts",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProformaInvoiceProducts_LastDeletedById",
                table: "ProformaInvoiceProducts",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProformaInvoiceProducts_LastUpdatedById",
                table: "ProformaInvoiceProducts",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProformaInvoiceProducts_ProductId",
                table: "ProformaInvoiceProducts",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProformaInvoiceProducts_ProformaInvoiceId",
                table: "ProformaInvoiceProducts",
                column: "ProformaInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ProformaInvoices_CreatedById",
                table: "ProformaInvoices",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProformaInvoices_LastDeletedById",
                table: "ProformaInvoices",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProformaInvoices_LastUpdatedById",
                table: "ProformaInvoices",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProformaInvoices_ProductionOrderId",
                table: "ProformaInvoices",
                column: "ProductionOrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_Inventories_NonProductionSuppliers_NonProductionSupplierId",
                table: "Inventories",
                column: "NonProductionSupplierId",
                principalTable: "NonProductionSuppliers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Inventories_NonProductionSuppliers_NonProductionSupplierId",
                table: "Inventories");

            migrationBuilder.DropTable(
                name: "Invoices");

            migrationBuilder.DropTable(
                name: "NonProductionSuppliers");

            migrationBuilder.DropTable(
                name: "ProformaInvoiceProducts");

            migrationBuilder.DropTable(
                name: "ProformaInvoices");

            migrationBuilder.DropIndex(
                name: "IX_Inventories_NonProductionSupplierId",
                table: "Inventories");

            migrationBuilder.DropColumn(
                name: "NonProductionSupplierId",
                table: "Inventories");
        }
    }
}
