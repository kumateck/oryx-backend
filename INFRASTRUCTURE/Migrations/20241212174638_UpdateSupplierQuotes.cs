using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSupplierQuotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Processed",
                table: "SourceRequisitionItemSuppliers");

            migrationBuilder.DropColumn(
                name: "ReceivedQuotationAt",
                table: "SourceRequisitionItemSuppliers");

            migrationBuilder.DropColumn(
                name: "SupplierQuotedPrice",
                table: "SourceRequisitionItemSuppliers");

            migrationBuilder.CreateTable(
                name: "SupplierQuotations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceivedQuotation = table.Column<bool>(type: "boolean", nullable: false),
                    Processed = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierQuotations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierQuotations_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SupplierQuotations_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SupplierQuotations_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SupplierQuotations_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SupplierQuotationItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierQuotationId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    UoMId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    QuotedPrice = table.Column<decimal>(type: "numeric", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierQuotationItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierQuotationItems_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SupplierQuotationItems_SupplierQuotations_SupplierQuotation~",
                        column: x => x.SupplierQuotationId,
                        principalTable: "SupplierQuotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SupplierQuotationItems_UnitOfMeasures_UoMId",
                        column: x => x.UoMId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SupplierQuotationItems_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SupplierQuotationItems_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SupplierQuotationItems_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierQuotationItems_CreatedById",
                table: "SupplierQuotationItems",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierQuotationItems_LastDeletedById",
                table: "SupplierQuotationItems",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierQuotationItems_LastUpdatedById",
                table: "SupplierQuotationItems",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierQuotationItems_MaterialId",
                table: "SupplierQuotationItems",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierQuotationItems_SupplierQuotationId",
                table: "SupplierQuotationItems",
                column: "SupplierQuotationId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierQuotationItems_UoMId",
                table: "SupplierQuotationItems",
                column: "UoMId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierQuotations_CreatedById",
                table: "SupplierQuotations",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierQuotations_LastDeletedById",
                table: "SupplierQuotations",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierQuotations_LastUpdatedById",
                table: "SupplierQuotations",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierQuotations_SupplierId",
                table: "SupplierQuotations",
                column: "SupplierId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SupplierQuotationItems");

            migrationBuilder.DropTable(
                name: "SupplierQuotations");

            migrationBuilder.AddColumn<bool>(
                name: "Processed",
                table: "SourceRequisitionItemSuppliers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReceivedQuotationAt",
                table: "SourceRequisitionItemSuppliers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SupplierQuotedPrice",
                table: "SourceRequisitionItemSuppliers",
                type: "numeric",
                nullable: true);
        }
    }
}
