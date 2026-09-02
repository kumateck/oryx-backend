using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddCashflowPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DueDays",
                table: "TermsOfPayments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DueDate",
                table: "ShipmentInvoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DueDate",
                table: "PurchaseOrderInvoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DueDate",
                table: "Invoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TermsOfPaymentId",
                table: "Invoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBaseCurrency",
                table: "Currencies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DueDate",
                table: "BillingSheets",
                type: "timestamp with time zone",
                nullable: true);

            // Infer only explicit and unambiguous legacy values. Ambiguous production
            // data remains null and is surfaced by the reporting data-quality warnings.
            migrationBuilder.Sql(
                """
                UPDATE "TermsOfPayments"
                SET "DueDays" = CASE
                    WHEN lower(trim("Name")) IN ('due on receipt', 'due upon receipt', 'cash on delivery') THEN 0
                    WHEN trim("Name") ~* '^net[[:space:]]*[0-9]+$'
                        THEN substring(trim("Name") from '[0-9]+')::integer
                    ELSE NULL
                END
                WHERE "DueDays" IS NULL;

                UPDATE "ShipmentInvoices" AS invoice
                SET "CurrencyId" = inferred."CurrencyId"
                FROM (
                    SELECT item."ShipmentInvoiceId", min(item."CurrencyId"::text)::uuid AS "CurrencyId"
                    FROM "ShipmentInvoiceItems" AS item
                    GROUP BY item."ShipmentInvoiceId"
                    HAVING count(*) = count(item."CurrencyId")
                       AND count(DISTINCT item."CurrencyId") = 1
                ) AS inferred
                WHERE invoice."Id" = inferred."ShipmentInvoiceId"
                  AND invoice."CurrencyId" IS NULL;

                UPDATE "PurchaseOrderInvoices" AS invoice
                SET "DueDate" = invoice."CreatedAt" + (terms."DueDays" * interval '1 day')
                FROM "PurchaseOrders" AS purchase_order
                JOIN "TermsOfPayments" AS terms ON terms."Id" = purchase_order."TermsOfPaymentId"
                WHERE invoice."PurchaseOrderId" = purchase_order."Id"
                  AND invoice."DueDate" IS NULL
                  AND terms."DueDays" IS NOT NULL;

                UPDATE "ShipmentInvoices" AS invoice
                SET "DueDate" = invoice."CreatedAt" + (inferred."DueDays" * interval '1 day')
                FROM (
                    SELECT item."ShipmentInvoiceId", min(terms."DueDays") AS "DueDays"
                    FROM "ShipmentInvoiceItems" AS item
                    JOIN "PurchaseOrders" AS purchase_order ON purchase_order."Id" = item."PurchaseOrderId"
                    LEFT JOIN "TermsOfPayments" AS terms ON terms."Id" = purchase_order."TermsOfPaymentId"
                    GROUP BY item."ShipmentInvoiceId"
                    HAVING count(*) = count(terms."DueDays")
                       AND count(DISTINCT terms."DueDays") = 1
                ) AS inferred
                WHERE invoice."Id" = inferred."ShipmentInvoiceId"
                  AND invoice."DueDate" IS NULL;

                UPDATE "BillingSheets" AS sheet
                SET "DueDate" = sheet."ExpectedArrivalDate" + (inferred."DueDays" * interval '1 day')
                FROM (
                    SELECT item."ShipmentInvoiceId", min(terms."DueDays") AS "DueDays"
                    FROM "ShipmentInvoiceItems" AS item
                    JOIN "PurchaseOrders" AS purchase_order ON purchase_order."Id" = item."PurchaseOrderId"
                    LEFT JOIN "TermsOfPayments" AS terms ON terms."Id" = purchase_order."TermsOfPaymentId"
                    GROUP BY item."ShipmentInvoiceId"
                    HAVING count(*) = count(terms."DueDays")
                       AND count(DISTINCT terms."DueDays") = 1
                ) AS inferred
                WHERE sheet."InvoiceId" = inferred."ShipmentInvoiceId"
                  AND sheet."DueDate" IS NULL;
                """
            );

            migrationBuilder.CreateTable(
                name: "ExchangeRates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrencyId = table.Column<Guid>(type: "uuid", nullable: false),
                    RateToBase = table.Column<decimal>(type: "numeric", nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExchangeRates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExchangeRates_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExchangeRates_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ExchangeRates_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ExchangeRates_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "InvoiceAmounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrencyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceAmounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceAmounts_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InvoiceAmounts_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InvoiceAmounts_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InvoiceAmounts_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InvoiceAmounts_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    CurrencyId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Method = table.Column<int>(type: "integer", nullable: false),
                    Reference = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RecordedById = table.Column<Guid>(type: "uuid", nullable: false),
                    PayableType = table.Column<int>(type: "integer", nullable: false),
                    PayableId = table.Column<Guid>(type: "uuid", nullable: false),
                    Approved = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payments_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payments_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Payments_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Payments_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Payments_users_RecordedById",
                        column: x => x.RecordedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentApprovals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_PaymentApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentApprovals_Approvals_ApprovalId",
                        column: x => x.ApprovalId,
                        principalTable: "Approvals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PaymentApprovals_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PaymentApprovals_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PaymentApprovals_users_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PaymentApprovals_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_TermsOfPaymentId",
                table: "Invoices",
                column: "TermsOfPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRates_CreatedById",
                table: "ExchangeRates",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRates_CurrencyId_EffectiveDate",
                table: "ExchangeRates",
                columns: new[] { "CurrencyId", "EffectiveDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRates_LastDeletedById",
                table: "ExchangeRates",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRates_LastUpdatedById",
                table: "ExchangeRates",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceAmounts_CreatedById",
                table: "InvoiceAmounts",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceAmounts_CurrencyId",
                table: "InvoiceAmounts",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceAmounts_InvoiceId_CurrencyId",
                table: "InvoiceAmounts",
                columns: new[] { "InvoiceId", "CurrencyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceAmounts_LastDeletedById",
                table: "InvoiceAmounts",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceAmounts_LastUpdatedById",
                table: "InvoiceAmounts",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentApprovals_ApprovalId_PaymentId_Order_UserId_RoleId",
                table: "PaymentApprovals",
                columns: new[] { "ApprovalId", "PaymentId", "Order", "UserId", "RoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentApprovals_ApprovedById",
                table: "PaymentApprovals",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentApprovals_PaymentId",
                table: "PaymentApprovals",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentApprovals_RoleId",
                table: "PaymentApprovals",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentApprovals_UserId",
                table: "PaymentApprovals",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CreatedById",
                table: "Payments",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CurrencyId",
                table: "Payments",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_LastDeletedById",
                table: "Payments",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_LastUpdatedById",
                table: "Payments",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PayableType_PayableId_Reference",
                table: "Payments",
                columns: new[] { "PayableType", "PayableId", "Reference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_RecordedById",
                table: "Payments",
                column: "RecordedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_TermsOfPayments_TermsOfPaymentId",
                table: "Invoices",
                column: "TermsOfPaymentId",
                principalTable: "TermsOfPayments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_TermsOfPayments_TermsOfPaymentId",
                table: "Invoices");

            migrationBuilder.DropTable(
                name: "ExchangeRates");

            migrationBuilder.DropTable(
                name: "InvoiceAmounts");

            migrationBuilder.DropTable(
                name: "PaymentApprovals");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_TermsOfPaymentId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "DueDays",
                table: "TermsOfPayments");

            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "ShipmentInvoices");

            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "PurchaseOrderInvoices");

            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "TermsOfPaymentId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "IsBaseCurrency",
                table: "Currencies");

            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "BillingSheets");
        }
    }
}
