using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class CatchUpPendingModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PromisedDeliveryDate",
                table: "ProductionOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceCustomerQuotationId",
                table: "ProductionOrders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountPercent",
                table: "ProductionOrderProducts",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitPrice",
                table: "ProductionOrderProducts",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UoMId",
                table: "ProductionOrderProducts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BillingAddress",
                table: "Customers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CreditLimit",
                table: "Customers",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CurrencyId",
                table: "Customers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingAddress",
                table: "Customers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TermsOfPaymentId",
                table: "Customers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Customers",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CustomerContacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Role = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerContacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerContacts_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerContacts_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CustomerContacts_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CustomerContacts_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CustomerPricingAgreements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    UoMId = table.Column<Guid>(type: "uuid", nullable: false),
                    AgreedPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    CurrencyId = table.Column<Guid>(type: "uuid", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerPricingAgreements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerPricingAgreements_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerPricingAgreements_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerPricingAgreements_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerPricingAgreements_UnitOfMeasures_UoMId",
                        column: x => x.UoMId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerPricingAgreements_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CustomerPricingAgreements_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CustomerPricingAgreements_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CustomerQuotations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrencyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ValidUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("PK_CustomerQuotations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerQuotations_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerQuotations_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerQuotations_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CustomerQuotations_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CustomerQuotations_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CustomerQuotationApprovals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerQuotationId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_CustomerQuotationApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerQuotationApprovals_Approvals_ApprovalId",
                        column: x => x.ApprovalId,
                        principalTable: "Approvals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomerQuotationApprovals_CustomerQuotations_CustomerQuota~",
                        column: x => x.CustomerQuotationId,
                        principalTable: "CustomerQuotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomerQuotationApprovals_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CustomerQuotationApprovals_users_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CustomerQuotationApprovals_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CustomerQuotationItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerQuotationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UoMId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerQuotationItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerQuotationItems_CustomerQuotations_CustomerQuotation~",
                        column: x => x.CustomerQuotationId,
                        principalTable: "CustomerQuotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomerQuotationItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomerQuotationItems_UnitOfMeasures_UoMId",
                        column: x => x.UoMId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomerQuotationItems_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CustomerQuotationItems_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CustomerQuotationItems_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrders_SourceCustomerQuotationId",
                table: "ProductionOrders",
                column: "SourceCustomerQuotationId",
                unique: true,
                filter: "\"SourceCustomerQuotationId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrderProducts_UoMId",
                table: "ProductionOrderProducts",
                column: "UoMId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_CurrencyId",
                table: "Customers",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_TermsOfPaymentId",
                table: "Customers",
                column: "TermsOfPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerContacts_CreatedById",
                table: "CustomerContacts",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerContacts_CustomerId",
                table: "CustomerContacts",
                column: "CustomerId",
                unique: true,
                filter: "\"DeletedAt\" IS NULL AND \"IsPrimary\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerContacts_LastDeletedById",
                table: "CustomerContacts",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerContacts_LastUpdatedById",
                table: "CustomerContacts",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPricingAgreements_CreatedById",
                table: "CustomerPricingAgreements",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPricingAgreements_CurrencyId",
                table: "CustomerPricingAgreements",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPricingAgreements_CustomerId_ProductId_UoMId_Effect~",
                table: "CustomerPricingAgreements",
                columns: new[] { "CustomerId", "ProductId", "UoMId", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPricingAgreements_LastDeletedById",
                table: "CustomerPricingAgreements",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPricingAgreements_LastUpdatedById",
                table: "CustomerPricingAgreements",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPricingAgreements_ProductId",
                table: "CustomerPricingAgreements",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPricingAgreements_UoMId",
                table: "CustomerPricingAgreements",
                column: "UoMId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerQuotationApprovals_ApprovalId_CustomerQuotationId_O~",
                table: "CustomerQuotationApprovals",
                columns: new[] { "ApprovalId", "CustomerQuotationId", "Order", "UserId", "RoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerQuotationApprovals_ApprovedById",
                table: "CustomerQuotationApprovals",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerQuotationApprovals_CustomerQuotationId",
                table: "CustomerQuotationApprovals",
                column: "CustomerQuotationId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerQuotationApprovals_RoleId",
                table: "CustomerQuotationApprovals",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerQuotationApprovals_UserId",
                table: "CustomerQuotationApprovals",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerQuotationItems_CreatedById",
                table: "CustomerQuotationItems",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerQuotationItems_CustomerQuotationId",
                table: "CustomerQuotationItems",
                column: "CustomerQuotationId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerQuotationItems_LastDeletedById",
                table: "CustomerQuotationItems",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerQuotationItems_LastUpdatedById",
                table: "CustomerQuotationItems",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerQuotationItems_ProductId",
                table: "CustomerQuotationItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerQuotationItems_UoMId",
                table: "CustomerQuotationItems",
                column: "UoMId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerQuotations_Code",
                table: "CustomerQuotations",
                column: "Code",
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerQuotations_CreatedById",
                table: "CustomerQuotations",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerQuotations_CurrencyId",
                table: "CustomerQuotations",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerQuotations_CustomerId",
                table: "CustomerQuotations",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerQuotations_LastDeletedById",
                table: "CustomerQuotations",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerQuotations_LastUpdatedById",
                table: "CustomerQuotations",
                column: "LastUpdatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_Currencies_CurrencyId",
                table: "Customers",
                column: "CurrencyId",
                principalTable: "Currencies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_TermsOfPayments_TermsOfPaymentId",
                table: "Customers",
                column: "TermsOfPaymentId",
                principalTable: "TermsOfPayments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionOrderProducts_UnitOfMeasures_UoMId",
                table: "ProductionOrderProducts",
                column: "UoMId",
                principalTable: "UnitOfMeasures",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionOrders_CustomerQuotations_SourceCustomerQuotation~",
                table: "ProductionOrders",
                column: "SourceCustomerQuotationId",
                principalTable: "CustomerQuotations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Customers_Currencies_CurrencyId",
                table: "Customers");

            migrationBuilder.DropForeignKey(
                name: "FK_Customers_TermsOfPayments_TermsOfPaymentId",
                table: "Customers");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionOrderProducts_UnitOfMeasures_UoMId",
                table: "ProductionOrderProducts");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionOrders_CustomerQuotations_SourceCustomerQuotation~",
                table: "ProductionOrders");

            migrationBuilder.DropTable(
                name: "CustomerContacts");

            migrationBuilder.DropTable(
                name: "CustomerPricingAgreements");

            migrationBuilder.DropTable(
                name: "CustomerQuotationApprovals");

            migrationBuilder.DropTable(
                name: "CustomerQuotationItems");

            migrationBuilder.DropTable(
                name: "CustomerQuotations");

            migrationBuilder.DropIndex(
                name: "IX_ProductionOrders_SourceCustomerQuotationId",
                table: "ProductionOrders");

            migrationBuilder.DropIndex(
                name: "IX_ProductionOrderProducts_UoMId",
                table: "ProductionOrderProducts");

            migrationBuilder.DropIndex(
                name: "IX_Customers_CurrencyId",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_TermsOfPaymentId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "PromisedDeliveryDate",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "SourceCustomerQuotationId",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "DiscountPercent",
                table: "ProductionOrderProducts");

            migrationBuilder.DropColumn(
                name: "UnitPrice",
                table: "ProductionOrderProducts");

            migrationBuilder.DropColumn(
                name: "UoMId",
                table: "ProductionOrderProducts");

            migrationBuilder.DropColumn(
                name: "BillingAddress",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "CreditLimit",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "CurrencyId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "ShippingAddress",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "TermsOfPaymentId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Customers");
        }
    }
}
