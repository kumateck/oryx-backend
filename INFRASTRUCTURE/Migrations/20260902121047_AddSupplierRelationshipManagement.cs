using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierRelationshipManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "Suppliers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RequalificationDueDate",
                table: "Suppliers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SupplierBankDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    BankName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    AccountNumber = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    AccountName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    SwiftCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Iban = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BranchAddress = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_SupplierBankDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierBankDetails_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierBankDetails_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierBankDetails_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SupplierBankDetails_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SupplierBankDetails_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SupplierCertifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    CertificationType = table.Column<int>(type: "integer", nullable: false),
                    CertificateNumber = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    IssuingBody = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    IssueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AttachmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierCertifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierCertifications_Attachments_AttachmentId",
                        column: x => x.AttachmentId,
                        principalTable: "Attachments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierCertifications_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierCertifications_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SupplierCertifications_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SupplierCertifications_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SupplierContacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_SupplierContacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierContacts_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierContacts_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SupplierContacts_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SupplierContacts_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SupplierPerformanceRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OnTimeDeliveryRate = table.Column<decimal>(type: "numeric", nullable: false),
                    QualityRejectRate = table.Column<decimal>(type: "numeric", nullable: false),
                    Score = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierPerformanceRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierPerformanceRecords_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierPerformanceRecords_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SupplierPerformanceRecords_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SupplierPerformanceRecords_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SupplierPricingAgreements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    UoMId = table.Column<Guid>(type: "uuid", nullable: false),
                    AgreedPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    PriceUoM = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
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
                    table.PrimaryKey("PK_SupplierPricingAgreements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierPricingAgreements_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierPricingAgreements_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierPricingAgreements_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierPricingAgreements_UnitOfMeasures_UoMId",
                        column: x => x.UoMId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierPricingAgreements_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SupplierPricingAgreements_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SupplierPricingAgreements_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierBankDetails_CreatedById",
                table: "SupplierBankDetails",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierBankDetails_CurrencyId",
                table: "SupplierBankDetails",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierBankDetails_LastDeletedById",
                table: "SupplierBankDetails",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierBankDetails_LastUpdatedById",
                table: "SupplierBankDetails",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierBankDetails_SupplierId_AccountNumber_CurrencyId",
                table: "SupplierBankDetails",
                columns: new[] { "SupplierId", "AccountNumber", "CurrencyId" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierCertifications_AttachmentId",
                table: "SupplierCertifications",
                column: "AttachmentId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierCertifications_CreatedById",
                table: "SupplierCertifications",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierCertifications_LastDeletedById",
                table: "SupplierCertifications",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierCertifications_LastUpdatedById",
                table: "SupplierCertifications",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierCertifications_SupplierId_CertificateNumber",
                table: "SupplierCertifications",
                columns: new[] { "SupplierId", "CertificateNumber" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierContacts_CreatedById",
                table: "SupplierContacts",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierContacts_LastDeletedById",
                table: "SupplierContacts",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierContacts_LastUpdatedById",
                table: "SupplierContacts",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierContacts_SupplierId",
                table: "SupplierContacts",
                column: "SupplierId",
                unique: true,
                filter: "\"DeletedAt\" IS NULL AND \"IsPrimary\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPerformanceRecords_CreatedById",
                table: "SupplierPerformanceRecords",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPerformanceRecords_LastDeletedById",
                table: "SupplierPerformanceRecords",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPerformanceRecords_LastUpdatedById",
                table: "SupplierPerformanceRecords",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPerformanceRecords_SupplierId_PeriodStart_PeriodEnd",
                table: "SupplierPerformanceRecords",
                columns: new[] { "SupplierId", "PeriodStart", "PeriodEnd" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPricingAgreements_CreatedById",
                table: "SupplierPricingAgreements",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPricingAgreements_CurrencyId",
                table: "SupplierPricingAgreements",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPricingAgreements_LastDeletedById",
                table: "SupplierPricingAgreements",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPricingAgreements_LastUpdatedById",
                table: "SupplierPricingAgreements",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPricingAgreements_MaterialId",
                table: "SupplierPricingAgreements",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPricingAgreements_SupplierId_MaterialId_UoMId_Effec~",
                table: "SupplierPricingAgreements",
                columns: new[] { "SupplierId", "MaterialId", "UoMId", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPricingAgreements_UoMId",
                table: "SupplierPricingAgreements",
                column: "UoMId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SupplierBankDetails");

            migrationBuilder.DropTable(
                name: "SupplierCertifications");

            migrationBuilder.DropTable(
                name: "SupplierContacts");

            migrationBuilder.DropTable(
                name: "SupplierPerformanceRecords");

            migrationBuilder.DropTable(
                name: "SupplierPricingAgreements");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "RequalificationDueDate",
                table: "Suppliers");
        }
    }
}
