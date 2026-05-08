using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddItemProcurementProcessAndGrn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ItemGrns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "text", nullable: true),
                    QuantityReceived = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemGrns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemGrns_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemGrns_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemGrns_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ItemGrns_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ItemGrns_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ItemShipmentInvoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: true),
                    TotalCost = table.Column<decimal>(type: "numeric", nullable: false),
                    CurrencyId = table.Column<Guid>(type: "uuid", nullable: true),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemShipmentInvoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemShipmentInvoices_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ItemShipmentInvoices_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ItemShipmentInvoices_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ItemShipmentInvoices_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ItemShipmentInvoices_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ItemBillingSheets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    BillOfLading = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: true),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpectedArrivalDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FreeTimeExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FreeTimeDuration = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DemurrageStartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ContainerNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    NumberOfPackages = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ContainerPackageStyleId = table.Column<Guid>(type: "uuid", nullable: true),
                    PackageDescription = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemBillingSheets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemBillingSheets_ItemShipmentInvoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "ItemShipmentInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemBillingSheets_PackageStyles_ContainerPackageStyleId",
                        column: x => x.ContainerPackageStyleId,
                        principalTable: "PackageStyles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ItemBillingSheets_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ItemBillingSheets_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ItemBillingSheets_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ItemBillingSheets_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ItemShipmentDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ItemShipmentInvoiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    ArrivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClearedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TransitStartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AtPortAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Approved = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemShipmentDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemShipmentDocuments_ItemShipmentInvoices_ItemShipmentInvo~",
                        column: x => x.ItemShipmentInvoiceId,
                        principalTable: "ItemShipmentInvoices",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ItemShipmentDocuments_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ItemShipmentDocuments_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ItemShipmentDocuments_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ItemShipmentInvoiceItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemShipmentInvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    UoMId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpectedQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    ReceivedQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    Reason = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    TotalCost = table.Column<decimal>(type: "numeric", nullable: false),
                    CurrencyId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemShipmentInvoiceItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemShipmentInvoiceItems_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ItemShipmentInvoiceItems_ItemShipmentInvoices_ItemShipmentI~",
                        column: x => x.ItemShipmentInvoiceId,
                        principalTable: "ItemShipmentInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemShipmentInvoiceItems_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemShipmentInvoiceItems_UnitOfMeasures_UoMId",
                        column: x => x.UoMId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemShipmentInvoiceItems_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ItemShipmentInvoiceItems_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ItemShipmentInvoiceItems_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ItemBillingSheetCharges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChargeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemBillingSheetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Paid = table.Column<bool>(type: "boolean", nullable: false),
                    CurrencyId = table.Column<Guid>(type: "uuid", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemBillingSheetCharges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemBillingSheetCharges_Charges_ChargeId",
                        column: x => x.ChargeId,
                        principalTable: "Charges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemBillingSheetCharges_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ItemBillingSheetCharges_ItemBillingSheets_ItemBillingSheetId",
                        column: x => x.ItemBillingSheetId,
                        principalTable: "ItemBillingSheets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ItemBillingSheetCharges_ChargeId",
                table: "ItemBillingSheetCharges",
                column: "ChargeId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemBillingSheetCharges_CurrencyId",
                table: "ItemBillingSheetCharges",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemBillingSheetCharges_ItemBillingSheetId",
                table: "ItemBillingSheetCharges",
                column: "ItemBillingSheetId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemBillingSheets_ContainerPackageStyleId",
                table: "ItemBillingSheets",
                column: "ContainerPackageStyleId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemBillingSheets_CreatedById",
                table: "ItemBillingSheets",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ItemBillingSheets_InvoiceId",
                table: "ItemBillingSheets",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemBillingSheets_LastDeletedById",
                table: "ItemBillingSheets",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ItemBillingSheets_LastUpdatedById",
                table: "ItemBillingSheets",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ItemBillingSheets_SupplierId",
                table: "ItemBillingSheets",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemGrns_CreatedById",
                table: "ItemGrns",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ItemGrns_ItemId",
                table: "ItemGrns",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemGrns_LastDeletedById",
                table: "ItemGrns",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ItemGrns_LastUpdatedById",
                table: "ItemGrns",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ItemGrns_SupplierId",
                table: "ItemGrns",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemShipmentDocuments_CreatedById",
                table: "ItemShipmentDocuments",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ItemShipmentDocuments_ItemShipmentInvoiceId",
                table: "ItemShipmentDocuments",
                column: "ItemShipmentInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemShipmentDocuments_LastDeletedById",
                table: "ItemShipmentDocuments",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ItemShipmentDocuments_LastUpdatedById",
                table: "ItemShipmentDocuments",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ItemShipmentInvoiceItems_CreatedById",
                table: "ItemShipmentInvoiceItems",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ItemShipmentInvoiceItems_CurrencyId",
                table: "ItemShipmentInvoiceItems",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemShipmentInvoiceItems_ItemId",
                table: "ItemShipmentInvoiceItems",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemShipmentInvoiceItems_ItemShipmentInvoiceId",
                table: "ItemShipmentInvoiceItems",
                column: "ItemShipmentInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemShipmentInvoiceItems_LastDeletedById",
                table: "ItemShipmentInvoiceItems",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ItemShipmentInvoiceItems_LastUpdatedById",
                table: "ItemShipmentInvoiceItems",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ItemShipmentInvoiceItems_UoMId",
                table: "ItemShipmentInvoiceItems",
                column: "UoMId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemShipmentInvoices_CreatedById",
                table: "ItemShipmentInvoices",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ItemShipmentInvoices_CurrencyId",
                table: "ItemShipmentInvoices",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemShipmentInvoices_LastDeletedById",
                table: "ItemShipmentInvoices",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ItemShipmentInvoices_LastUpdatedById",
                table: "ItemShipmentInvoices",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ItemShipmentInvoices_SupplierId",
                table: "ItemShipmentInvoices",
                column: "SupplierId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItemBillingSheetCharges");

            migrationBuilder.DropTable(
                name: "ItemGrns");

            migrationBuilder.DropTable(
                name: "ItemShipmentDocuments");

            migrationBuilder.DropTable(
                name: "ItemShipmentInvoiceItems");

            migrationBuilder.DropTable(
                name: "ItemBillingSheets");

            migrationBuilder.DropTable(
                name: "ItemShipmentInvoices");
        }
    }
}
