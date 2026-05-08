using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEquipmentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MachineId",
                table: "Equipments");

            migrationBuilder.RenameColumn(
                name: "StorageLocation",
                table: "Equipments",
                newName: "SerialNumber");

            migrationBuilder.AddColumn<Guid>(
                name: "ServiceProformaInvoiceId",
                table: "JobOrders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EquipmentNumber",
                table: "Equipments",
                type: "character varying(100000)",
                maxLength: 100000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Equipments",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Model",
                table: "Equipments",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ServiceProformaInvoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    JobOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceQuotationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RequestedById = table.Column<Guid>(type: "uuid", nullable: false),
                    ResponseReceivedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ServiceCharge = table.Column<decimal>(type: "numeric", nullable: false),
                    CurrencyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ResponseNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ProformaInvoiceDocumentUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceProformaInvoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceProformaInvoices_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServiceProformaInvoices_JobOrders_JobOrderId",
                        column: x => x.JobOrderId,
                        principalTable: "JobOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceProformaInvoices_ServiceProviders_ServiceProviderId",
                        column: x => x.ServiceProviderId,
                        principalTable: "ServiceProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServiceProformaInvoices_ServiceQuotations_ServiceQuotationId",
                        column: x => x.ServiceQuotationId,
                        principalTable: "ServiceQuotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServiceProformaInvoices_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ServiceProformaInvoices_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ServiceProformaInvoices_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ServiceProformaInvoices_users_RequestedById",
                        column: x => x.RequestedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceProformaInvoiceItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceProformaInvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    ItemName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    UnitOfMeasureId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    Supplier = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceProformaInvoiceItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceProformaInvoiceItems_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ServiceProformaInvoiceItems_ServiceProformaInvoices_Service~",
                        column: x => x.ServiceProformaInvoiceId,
                        principalTable: "ServiceProformaInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServiceProformaInvoiceItems_UnitOfMeasures_UnitOfMeasureId",
                        column: x => x.UnitOfMeasureId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServiceProformaInvoiceItems_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ServiceProformaInvoiceItems_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ServiceProformaInvoiceItems_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProformaInvoiceItems_CreatedById",
                table: "ServiceProformaInvoiceItems",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProformaInvoiceItems_ItemId",
                table: "ServiceProformaInvoiceItems",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProformaInvoiceItems_LastDeletedById",
                table: "ServiceProformaInvoiceItems",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProformaInvoiceItems_LastUpdatedById",
                table: "ServiceProformaInvoiceItems",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProformaInvoiceItems_ServiceProformaInvoiceId",
                table: "ServiceProformaInvoiceItems",
                column: "ServiceProformaInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProformaInvoiceItems_UnitOfMeasureId",
                table: "ServiceProformaInvoiceItems",
                column: "UnitOfMeasureId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProformaInvoices_CreatedById",
                table: "ServiceProformaInvoices",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProformaInvoices_CurrencyId",
                table: "ServiceProformaInvoices",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProformaInvoices_JobOrderId",
                table: "ServiceProformaInvoices",
                column: "JobOrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProformaInvoices_LastDeletedById",
                table: "ServiceProformaInvoices",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProformaInvoices_LastUpdatedById",
                table: "ServiceProformaInvoices",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProformaInvoices_RequestedById",
                table: "ServiceProformaInvoices",
                column: "RequestedById");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProformaInvoices_ServiceProviderId",
                table: "ServiceProformaInvoices",
                column: "ServiceProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProformaInvoices_ServiceQuotationId",
                table: "ServiceProformaInvoices",
                column: "ServiceQuotationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceProformaInvoiceItems");

            migrationBuilder.DropTable(
                name: "ServiceProformaInvoices");

            migrationBuilder.DropColumn(
                name: "ServiceProformaInvoiceId",
                table: "JobOrders");

            migrationBuilder.DropColumn(
                name: "EquipmentNumber",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "Model",
                table: "Equipments");

            migrationBuilder.RenameColumn(
                name: "SerialNumber",
                table: "Equipments",
                newName: "StorageLocation");

            migrationBuilder.AddColumn<string>(
                name: "MachineId",
                table: "Equipments",
                type: "text",
                nullable: true);
        }
    }
}
