using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryProcurementEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InventoryPurchaseRequisitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ExpectedDeliveryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Remarks = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
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
                    table.PrimaryKey("PK_InventoryPurchaseRequisitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryPurchaseRequisitions_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InventoryPurchaseRequisitions_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InventoryPurchaseRequisitions_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Memos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Memos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Memos_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Memos_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Memos_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "InventoryPurchaseRequisitionItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryPurchaseRequisitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    UoMId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_InventoryPurchaseRequisitionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryPurchaseRequisitionItems_InventoryPurchaseRequisit~",
                        column: x => x.InventoryPurchaseRequisitionId,
                        principalTable: "InventoryPurchaseRequisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InventoryPurchaseRequisitionItems_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InventoryPurchaseRequisitionItems_UnitOfMeasures_UoMId",
                        column: x => x.UoMId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InventoryPurchaseRequisitionItems_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InventoryPurchaseRequisitionItems_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InventoryPurchaseRequisitionItems_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SourceInventoryRequisitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryPurchaseRequisitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Remarks = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    SentQuotationRequestAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SourceInventoryRequisitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SourceInventoryRequisitions_InventoryPurchaseRequisitions_I~",
                        column: x => x.InventoryPurchaseRequisitionId,
                        principalTable: "InventoryPurchaseRequisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SourceInventoryRequisitions_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SourceInventoryRequisitions_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SourceInventoryRequisitions_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SourceInventoryRequisitions_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MarketRequisitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryPurchaseRequisitionItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    UoMId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_MarketRequisitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketRequisitions_InventoryPurchaseRequisitionItems_Invent~",
                        column: x => x.InventoryPurchaseRequisitionItemId,
                        principalTable: "InventoryPurchaseRequisitionItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MarketRequisitions_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MarketRequisitions_UnitOfMeasures_UoMId",
                        column: x => x.UoMId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MarketRequisitions_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MarketRequisitions_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MarketRequisitions_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SourceInventoryRequisitionItem",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceInventoryPurchaseRequisitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceInventoryRequisitionId = table.Column<Guid>(type: "uuid", nullable: true),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    UoMId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_SourceInventoryRequisitionItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SourceInventoryRequisitionItem_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SourceInventoryRequisitionItem_SourceInventoryRequisitions_~",
                        column: x => x.SourceInventoryRequisitionId,
                        principalTable: "SourceInventoryRequisitions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SourceInventoryRequisitionItem_UnitOfMeasures_UoMId",
                        column: x => x.UoMId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SourceInventoryRequisitionItem_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SourceInventoryRequisitionItem_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SourceInventoryRequisitionItem_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "VendorQuotations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceInventoryRequisitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceivedQuotation = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorQuotations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendorQuotations_SourceInventoryRequisitions_SourceInventor~",
                        column: x => x.SourceInventoryRequisitionId,
                        principalTable: "SourceInventoryRequisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VendorQuotations_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VendorQuotations_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VendorQuotations_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VendorQuotations_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MarketRequisitionVendors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MarketRequisitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorName = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    VendorAddress = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    VendorPhoneNumber = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PricePerUnit = table.Column<decimal>(type: "numeric", nullable: false),
                    ModeOfPayment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    TermsOfPaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeliveryMode = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    EstimatedDeliveryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Complete = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketRequisitionVendors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketRequisitionVendors_MarketRequisitions_MarketRequisiti~",
                        column: x => x.MarketRequisitionId,
                        principalTable: "MarketRequisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MarketRequisitionVendors_TermsOfPayments_TermsOfPaymentId",
                        column: x => x.TermsOfPaymentId,
                        principalTable: "TermsOfPayments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MarketRequisitionVendors_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MarketRequisitionVendors_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MarketRequisitionVendors_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "VendorQuotationItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorQuotationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    UoMId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    QuotedPrice = table.Column<decimal>(type: "numeric", nullable: true),
                    DeliveryMode = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    EstimatedDeliveryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TermsOfPaymentId = table.Column<Guid>(type: "uuid", nullable: true),
                    InventoryPurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorQuotationItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendorQuotationItems_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VendorQuotationItems_TermsOfPayments_TermsOfPaymentId",
                        column: x => x.TermsOfPaymentId,
                        principalTable: "TermsOfPayments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VendorQuotationItems_UnitOfMeasures_UoMId",
                        column: x => x.UoMId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VendorQuotationItems_VendorQuotations_VendorQuotationId",
                        column: x => x.VendorQuotationId,
                        principalTable: "VendorQuotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VendorQuotationItems_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VendorQuotationItems_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VendorQuotationItems_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MemoItem",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MemoId = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorQuotationItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    MarketRequisitionVendorId = table.Column<Guid>(type: "uuid", nullable: true),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    UoMId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    PricePerUnit = table.Column<decimal>(type: "numeric", nullable: false),
                    PurchasedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemoItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MemoItem_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MemoItem_MarketRequisitionVendors_MarketRequisitionVendorId",
                        column: x => x.MarketRequisitionVendorId,
                        principalTable: "MarketRequisitionVendors",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MemoItem_Memos_MemoId",
                        column: x => x.MemoId,
                        principalTable: "Memos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MemoItem_UnitOfMeasures_UoMId",
                        column: x => x.UoMId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MemoItem_VendorQuotationItems_VendorQuotationItemId",
                        column: x => x.VendorQuotationItemId,
                        principalTable: "VendorQuotationItems",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MemoItem_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MemoItem_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MemoItem_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryPurchaseRequisitionItems_CreatedById",
                table: "InventoryPurchaseRequisitionItems",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryPurchaseRequisitionItems_InventoryPurchaseRequisit~",
                table: "InventoryPurchaseRequisitionItems",
                column: "InventoryPurchaseRequisitionId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryPurchaseRequisitionItems_ItemId",
                table: "InventoryPurchaseRequisitionItems",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryPurchaseRequisitionItems_LastDeletedById",
                table: "InventoryPurchaseRequisitionItems",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryPurchaseRequisitionItems_LastUpdatedById",
                table: "InventoryPurchaseRequisitionItems",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryPurchaseRequisitionItems_UoMId",
                table: "InventoryPurchaseRequisitionItems",
                column: "UoMId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryPurchaseRequisitions_CreatedById",
                table: "InventoryPurchaseRequisitions",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryPurchaseRequisitions_LastDeletedById",
                table: "InventoryPurchaseRequisitions",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryPurchaseRequisitions_LastUpdatedById",
                table: "InventoryPurchaseRequisitions",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_MarketRequisitions_CreatedById",
                table: "MarketRequisitions",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_MarketRequisitions_InventoryPurchaseRequisitionItemId",
                table: "MarketRequisitions",
                column: "InventoryPurchaseRequisitionItemId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketRequisitions_ItemId",
                table: "MarketRequisitions",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketRequisitions_LastDeletedById",
                table: "MarketRequisitions",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_MarketRequisitions_LastUpdatedById",
                table: "MarketRequisitions",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_MarketRequisitions_UoMId",
                table: "MarketRequisitions",
                column: "UoMId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketRequisitionVendors_CreatedById",
                table: "MarketRequisitionVendors",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_MarketRequisitionVendors_LastDeletedById",
                table: "MarketRequisitionVendors",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_MarketRequisitionVendors_LastUpdatedById",
                table: "MarketRequisitionVendors",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_MarketRequisitionVendors_MarketRequisitionId",
                table: "MarketRequisitionVendors",
                column: "MarketRequisitionId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketRequisitionVendors_TermsOfPaymentId",
                table: "MarketRequisitionVendors",
                column: "TermsOfPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_MemoItem_CreatedById",
                table: "MemoItem",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_MemoItem_ItemId",
                table: "MemoItem",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_MemoItem_LastDeletedById",
                table: "MemoItem",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_MemoItem_LastUpdatedById",
                table: "MemoItem",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_MemoItem_MarketRequisitionVendorId",
                table: "MemoItem",
                column: "MarketRequisitionVendorId");

            migrationBuilder.CreateIndex(
                name: "IX_MemoItem_MemoId",
                table: "MemoItem",
                column: "MemoId");

            migrationBuilder.CreateIndex(
                name: "IX_MemoItem_UoMId",
                table: "MemoItem",
                column: "UoMId");

            migrationBuilder.CreateIndex(
                name: "IX_MemoItem_VendorQuotationItemId",
                table: "MemoItem",
                column: "VendorQuotationItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Memos_CreatedById",
                table: "Memos",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Memos_LastDeletedById",
                table: "Memos",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Memos_LastUpdatedById",
                table: "Memos",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_SourceInventoryRequisitionItem_CreatedById",
                table: "SourceInventoryRequisitionItem",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_SourceInventoryRequisitionItem_ItemId",
                table: "SourceInventoryRequisitionItem",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SourceInventoryRequisitionItem_LastDeletedById",
                table: "SourceInventoryRequisitionItem",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_SourceInventoryRequisitionItem_LastUpdatedById",
                table: "SourceInventoryRequisitionItem",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_SourceInventoryRequisitionItem_SourceInventoryRequisitionId",
                table: "SourceInventoryRequisitionItem",
                column: "SourceInventoryRequisitionId");

            migrationBuilder.CreateIndex(
                name: "IX_SourceInventoryRequisitionItem_UoMId",
                table: "SourceInventoryRequisitionItem",
                column: "UoMId");

            migrationBuilder.CreateIndex(
                name: "IX_SourceInventoryRequisitions_CreatedById",
                table: "SourceInventoryRequisitions",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_SourceInventoryRequisitions_InventoryPurchaseRequisitionId",
                table: "SourceInventoryRequisitions",
                column: "InventoryPurchaseRequisitionId");

            migrationBuilder.CreateIndex(
                name: "IX_SourceInventoryRequisitions_LastDeletedById",
                table: "SourceInventoryRequisitions",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_SourceInventoryRequisitions_LastUpdatedById",
                table: "SourceInventoryRequisitions",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_SourceInventoryRequisitions_VendorId",
                table: "SourceInventoryRequisitions",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorQuotationItems_CreatedById",
                table: "VendorQuotationItems",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_VendorQuotationItems_ItemId",
                table: "VendorQuotationItems",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorQuotationItems_LastDeletedById",
                table: "VendorQuotationItems",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_VendorQuotationItems_LastUpdatedById",
                table: "VendorQuotationItems",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_VendorQuotationItems_TermsOfPaymentId",
                table: "VendorQuotationItems",
                column: "TermsOfPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorQuotationItems_UoMId",
                table: "VendorQuotationItems",
                column: "UoMId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorQuotationItems_VendorQuotationId",
                table: "VendorQuotationItems",
                column: "VendorQuotationId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorQuotations_CreatedById",
                table: "VendorQuotations",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_VendorQuotations_LastDeletedById",
                table: "VendorQuotations",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_VendorQuotations_LastUpdatedById",
                table: "VendorQuotations",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_VendorQuotations_SourceInventoryRequisitionId",
                table: "VendorQuotations",
                column: "SourceInventoryRequisitionId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorQuotations_VendorId",
                table: "VendorQuotations",
                column: "VendorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MemoItem");

            migrationBuilder.DropTable(
                name: "SourceInventoryRequisitionItem");

            migrationBuilder.DropTable(
                name: "MarketRequisitionVendors");

            migrationBuilder.DropTable(
                name: "Memos");

            migrationBuilder.DropTable(
                name: "VendorQuotationItems");

            migrationBuilder.DropTable(
                name: "MarketRequisitions");

            migrationBuilder.DropTable(
                name: "VendorQuotations");

            migrationBuilder.DropTable(
                name: "InventoryPurchaseRequisitionItems");

            migrationBuilder.DropTable(
                name: "SourceInventoryRequisitions");

            migrationBuilder.DropTable(
                name: "InventoryPurchaseRequisitions");
        }
    }
}
