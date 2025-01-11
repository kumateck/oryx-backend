using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class ShipmentInvoiceAndShipmentDiscrepancy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShipmentDiscrepancies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShipmentDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipmentDiscrepancies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShipmentDiscrepancies_ShipmentDocuments_ShipmentDocumentId",
                        column: x => x.ShipmentDocumentId,
                        principalTable: "ShipmentDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShipmentDiscrepancies_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ShipmentDiscrepancies_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ShipmentDiscrepancies_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ShipmentInvoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShipmentDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipmentInvoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShipmentInvoices_ShipmentDocuments_ShipmentDocumentId",
                        column: x => x.ShipmentDocumentId,
                        principalTable: "ShipmentDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShipmentInvoices_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ShipmentInvoices_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ShipmentInvoices_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ShipmentDiscrepancyItem",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShipmentDiscrepancyId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    UoMId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceivedQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    DiscrepancyType = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Resolved = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipmentDiscrepancyItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShipmentDiscrepancyItem_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShipmentDiscrepancyItem_ShipmentDiscrepancies_ShipmentDiscr~",
                        column: x => x.ShipmentDiscrepancyId,
                        principalTable: "ShipmentDiscrepancies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShipmentDiscrepancyItem_UnitOfMeasures_UoMId",
                        column: x => x.UoMId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShipmentDiscrepancyItem_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ShipmentDiscrepancyItem_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ShipmentDiscrepancyItem_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ShipmentInvoiceItem",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShipmentInvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    UoMId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpectedQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    ReceivedQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    Reason = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipmentInvoiceItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShipmentInvoiceItem_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShipmentInvoiceItem_ShipmentInvoices_ShipmentInvoiceId",
                        column: x => x.ShipmentInvoiceId,
                        principalTable: "ShipmentInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShipmentInvoiceItem_UnitOfMeasures_UoMId",
                        column: x => x.UoMId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShipmentInvoiceItem_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ShipmentInvoiceItem_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ShipmentInvoiceItem_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentDiscrepancies_CreatedById",
                table: "ShipmentDiscrepancies",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentDiscrepancies_LastDeletedById",
                table: "ShipmentDiscrepancies",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentDiscrepancies_LastUpdatedById",
                table: "ShipmentDiscrepancies",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentDiscrepancies_ShipmentDocumentId",
                table: "ShipmentDiscrepancies",
                column: "ShipmentDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentDiscrepancyItem_CreatedById",
                table: "ShipmentDiscrepancyItem",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentDiscrepancyItem_LastDeletedById",
                table: "ShipmentDiscrepancyItem",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentDiscrepancyItem_LastUpdatedById",
                table: "ShipmentDiscrepancyItem",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentDiscrepancyItem_MaterialId",
                table: "ShipmentDiscrepancyItem",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentDiscrepancyItem_ShipmentDiscrepancyId",
                table: "ShipmentDiscrepancyItem",
                column: "ShipmentDiscrepancyId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentDiscrepancyItem_UoMId",
                table: "ShipmentDiscrepancyItem",
                column: "UoMId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentInvoiceItem_CreatedById",
                table: "ShipmentInvoiceItem",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentInvoiceItem_LastDeletedById",
                table: "ShipmentInvoiceItem",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentInvoiceItem_LastUpdatedById",
                table: "ShipmentInvoiceItem",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentInvoiceItem_MaterialId",
                table: "ShipmentInvoiceItem",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentInvoiceItem_ShipmentInvoiceId",
                table: "ShipmentInvoiceItem",
                column: "ShipmentInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentInvoiceItem_UoMId",
                table: "ShipmentInvoiceItem",
                column: "UoMId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentInvoices_CreatedById",
                table: "ShipmentInvoices",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentInvoices_LastDeletedById",
                table: "ShipmentInvoices",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentInvoices_LastUpdatedById",
                table: "ShipmentInvoices",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentInvoices_ShipmentDocumentId",
                table: "ShipmentInvoices",
                column: "ShipmentDocumentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShipmentDiscrepancyItem");

            migrationBuilder.DropTable(
                name: "ShipmentInvoiceItem");

            migrationBuilder.DropTable(
                name: "ShipmentDiscrepancies");

            migrationBuilder.DropTable(
                name: "ShipmentInvoices");
        }
    }
}
