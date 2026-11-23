using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class HardenFinishedGoodsWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CustomerPoAttachmentId",
                table: "ProformaInvoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CustomerPoDate",
                table: "ProformaInvoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerPoNumber",
                table: "ProformaInvoices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CustomerPoVerifiedAt",
                table: "ProformaInvoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CustomerPoVerifiedById",
                table: "ProformaInvoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "InvoiceIssuedAt",
                table: "ProformaInvoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IssuedInvoiceCode",
                table: "ProformaInvoices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "IssuedInvoiceId",
                table: "ProformaInvoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryAddress",
                table: "ProductionOrderWaybills",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DriverName",
                table: "ProductionOrderWaybills",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DriverPhone",
                table: "ProductionOrderWaybills",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InvoiceId",
                table: "ProductionOrderWaybills",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "SealNumber",
                table: "ProductionOrderWaybills",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StorageConditions",
                table: "ProductionOrderWaybills",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Transporter",
                table: "ProductionOrderWaybills",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehicleRegistration",
                table: "ProductionOrderWaybills",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VoidReason",
                table: "ProductionOrderWaybills",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Voided",
                table: "ProductionOrderWaybills",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Invoices",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VoidReason",
                table: "Invoices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Voided",
                table: "Invoices",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "VoidedAt",
                table: "Invoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VoidedById",
                table: "Invoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AcceptedById",
                table: "FinishedGoodsTransferNotes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AdjustmentQuantity",
                table: "FinishedGoodsTransferNotes",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DispatchedQuantity",
                table: "FinishedGoodsTransferNotes",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "HoldReason",
                table: "FinishedGoodsTransferNotes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsOnHold",
                table: "FinishedGoodsTransferNotes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PackingStyleSnapshot",
                table: "FinishedGoodsTransferNotes",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PacksPerShipperSnapshot",
                table: "FinishedGoodsTransferNotes",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "QuarantinedReturnQuantity",
                table: "FinishedGoodsTransferNotes",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ReleasedReturnQuantity",
                table: "FinishedGoodsTransferNotes",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresStockReconciliation",
                table: "FinishedGoodsTransferNotes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "AllocateProductionOrders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "AllocateProductionOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CancelledById",
                table: "AllocateProductionOrders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CommercialClearanceReason",
                table: "AllocateProductionOrders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CommercialClearedAt",
                table: "AllocateProductionOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CommercialClearedById",
                table: "AllocateProductionOrders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerAuthorisationReference",
                table: "AllocateProductionOrders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryCondition",
                table: "AllocateProductionOrders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryEvidence",
                table: "AllocateProductionOrders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryReceiver",
                table: "AllocateProductionOrders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DispatchedAt",
                table: "AllocateProductionOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DispatchedById",
                table: "AllocateProductionOrders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExitReference",
                table: "AllocateProductionOrders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FefoExceptionReason",
                table: "AllocateProductionOrders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LoadedDriver",
                table: "AllocateProductionOrders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LoadedDriverPhone",
                table: "AllocateProductionOrders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LoadedVehicle",
                table: "AllocateProductionOrders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LoadingInspectionReference",
                table: "AllocateProductionOrders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LoadingLocation",
                table: "AllocateProductionOrders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinimumRemainingShelfLifeDays",
                table: "AllocateProductionOrders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "FinishedGoodsDeliveryLine",
                columns: table => new
                {
                    AllocateProductionOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FinishedGoodsTransferNoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcceptedYield = table.Column<decimal>(type: "numeric", nullable: false),
                    DamagedYield = table.Column<decimal>(type: "numeric", nullable: false),
                    ShortYield = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinishedGoodsDeliveryLine", x => new { x.AllocateProductionOrderId, x.Id });
                    table.ForeignKey(
                        name: "FK_FinishedGoodsDeliveryLine_AllocateProductionOrders_Allocate~",
                        column: x => x.AllocateProductionOrderId,
                        principalTable: "AllocateProductionOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FinishedGoodsRecalls",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchManufacturingRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RegulatoryClassification = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AuthorityReference = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Closed = table.Column<bool>(type: "boolean", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClosedById = table.Column<Guid>(type: "uuid", nullable: true),
                    ClosureReport = table.Column<string>(type: "text", nullable: true),
                    UnrecoverableYield = table.Column<decimal>(type: "numeric", nullable: false),
                    UnrecoverableReason = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinishedGoodsRecalls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinishedGoodsRecalls_BatchManufacturingRecords_BatchManufac~",
                        column: x => x.BatchManufacturingRecordId,
                        principalTable: "BatchManufacturingRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FinishedGoodsRecalls_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FinishedGoodsRecalls_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FinishedGoodsRecalls_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FinishedGoodsRecallContacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FinishedGoodsRecallId = table.Column<Guid>(type: "uuid", nullable: false),
                    AllocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    NotifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AcknowledgedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EffectivenessConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    Evidence = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinishedGoodsRecallContacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinishedGoodsRecallContacts_FinishedGoodsRecalls_FinishedGo~",
                        column: x => x.FinishedGoodsRecallId,
                        principalTable: "FinishedGoodsRecalls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FinishedGoodsRecallContacts_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FinishedGoodsRecallContacts_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FinishedGoodsRecallContacts_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FinishedGoodsRecallRecoveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FinishedGoodsRecallId = table.Column<Guid>(type: "uuid", nullable: false),
                    AllocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    FinishedGoodsTransferNoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    Evidence = table.Column<string>(type: "text", nullable: true),
                    Condition = table.Column<string>(type: "text", nullable: true),
                    Disposed = table.Column<bool>(type: "boolean", nullable: false),
                    DisposedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DisposedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DispositionEvidence = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinishedGoodsRecallRecoveries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinishedGoodsRecallRecoveries_FinishedGoodsRecalls_Finished~",
                        column: x => x.FinishedGoodsRecallId,
                        principalTable: "FinishedGoodsRecalls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FinishedGoodsRecallRecoveries_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FinishedGoodsRecallRecoveries_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FinishedGoodsRecallRecoveries_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_Code",
                table: "Invoices",
                column: "Code",
                unique: true,
                filter: "\"Code\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FinishedGoodsRecallContacts_CreatedById",
                table: "FinishedGoodsRecallContacts",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_FinishedGoodsRecallContacts_FinishedGoodsRecallId_Allocatio~",
                table: "FinishedGoodsRecallContacts",
                columns: new[] { "FinishedGoodsRecallId", "AllocationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinishedGoodsRecallContacts_LastDeletedById",
                table: "FinishedGoodsRecallContacts",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_FinishedGoodsRecallContacts_LastUpdatedById",
                table: "FinishedGoodsRecallContacts",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_FinishedGoodsRecallRecoveries_CreatedById",
                table: "FinishedGoodsRecallRecoveries",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_FinishedGoodsRecallRecoveries_FinishedGoodsRecallId",
                table: "FinishedGoodsRecallRecoveries",
                column: "FinishedGoodsRecallId");

            migrationBuilder.CreateIndex(
                name: "IX_FinishedGoodsRecallRecoveries_LastDeletedById",
                table: "FinishedGoodsRecallRecoveries",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_FinishedGoodsRecallRecoveries_LastUpdatedById",
                table: "FinishedGoodsRecallRecoveries",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_FinishedGoodsRecalls_BatchManufacturingRecordId",
                table: "FinishedGoodsRecalls",
                column: "BatchManufacturingRecordId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinishedGoodsRecalls_Code",
                table: "FinishedGoodsRecalls",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinishedGoodsRecalls_CreatedById",
                table: "FinishedGoodsRecalls",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_FinishedGoodsRecalls_LastDeletedById",
                table: "FinishedGoodsRecalls",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_FinishedGoodsRecalls_LastUpdatedById",
                table: "FinishedGoodsRecalls",
                column: "LastUpdatedById");
            // Historical stock is never inferred from old loaded/delivered flags. An operator
            // records the physical count, pack conversion and evidenced departures explicitly.
            migrationBuilder.Sql("UPDATE \"FinishedGoodsTransferNotes\" SET \"RequiresStockReconciliation\" = TRUE;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FinishedGoodsDeliveryLine");

            migrationBuilder.DropTable(
                name: "FinishedGoodsRecallContacts");

            migrationBuilder.DropTable(
                name: "FinishedGoodsRecallRecoveries");

            migrationBuilder.DropTable(
                name: "FinishedGoodsRecalls");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_Code",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "CustomerPoAttachmentId",
                table: "ProformaInvoices");

            migrationBuilder.DropColumn(
                name: "CustomerPoDate",
                table: "ProformaInvoices");

            migrationBuilder.DropColumn(
                name: "CustomerPoNumber",
                table: "ProformaInvoices");

            migrationBuilder.DropColumn(
                name: "CustomerPoVerifiedAt",
                table: "ProformaInvoices");

            migrationBuilder.DropColumn(
                name: "CustomerPoVerifiedById",
                table: "ProformaInvoices");

            migrationBuilder.DropColumn(
                name: "InvoiceIssuedAt",
                table: "ProformaInvoices");

            migrationBuilder.DropColumn(
                name: "IssuedInvoiceCode",
                table: "ProformaInvoices");

            migrationBuilder.DropColumn(
                name: "IssuedInvoiceId",
                table: "ProformaInvoices");

            migrationBuilder.DropColumn(
                name: "DeliveryAddress",
                table: "ProductionOrderWaybills");

            migrationBuilder.DropColumn(
                name: "DriverName",
                table: "ProductionOrderWaybills");

            migrationBuilder.DropColumn(
                name: "DriverPhone",
                table: "ProductionOrderWaybills");

            migrationBuilder.DropColumn(
                name: "InvoiceId",
                table: "ProductionOrderWaybills");

            migrationBuilder.DropColumn(
                name: "SealNumber",
                table: "ProductionOrderWaybills");

            migrationBuilder.DropColumn(
                name: "StorageConditions",
                table: "ProductionOrderWaybills");

            migrationBuilder.DropColumn(
                name: "Transporter",
                table: "ProductionOrderWaybills");

            migrationBuilder.DropColumn(
                name: "VehicleRegistration",
                table: "ProductionOrderWaybills");

            migrationBuilder.DropColumn(
                name: "VoidReason",
                table: "ProductionOrderWaybills");

            migrationBuilder.DropColumn(
                name: "Voided",
                table: "ProductionOrderWaybills");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "VoidReason",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "Voided",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "VoidedAt",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "VoidedById",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "AcceptedById",
                table: "FinishedGoodsTransferNotes");

            migrationBuilder.DropColumn(
                name: "AdjustmentQuantity",
                table: "FinishedGoodsTransferNotes");

            migrationBuilder.DropColumn(
                name: "DispatchedQuantity",
                table: "FinishedGoodsTransferNotes");

            migrationBuilder.DropColumn(
                name: "HoldReason",
                table: "FinishedGoodsTransferNotes");

            migrationBuilder.DropColumn(
                name: "IsOnHold",
                table: "FinishedGoodsTransferNotes");

            migrationBuilder.DropColumn(
                name: "PackingStyleSnapshot",
                table: "FinishedGoodsTransferNotes");

            migrationBuilder.DropColumn(
                name: "PacksPerShipperSnapshot",
                table: "FinishedGoodsTransferNotes");

            migrationBuilder.DropColumn(
                name: "QuarantinedReturnQuantity",
                table: "FinishedGoodsTransferNotes");

            migrationBuilder.DropColumn(
                name: "ReleasedReturnQuantity",
                table: "FinishedGoodsTransferNotes");

            migrationBuilder.DropColumn(
                name: "RequiresStockReconciliation",
                table: "FinishedGoodsTransferNotes");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "AllocateProductionOrders");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "AllocateProductionOrders");

            migrationBuilder.DropColumn(
                name: "CancelledById",
                table: "AllocateProductionOrders");

            migrationBuilder.DropColumn(
                name: "CommercialClearanceReason",
                table: "AllocateProductionOrders");

            migrationBuilder.DropColumn(
                name: "CommercialClearedAt",
                table: "AllocateProductionOrders");

            migrationBuilder.DropColumn(
                name: "CommercialClearedById",
                table: "AllocateProductionOrders");

            migrationBuilder.DropColumn(
                name: "CustomerAuthorisationReference",
                table: "AllocateProductionOrders");

            migrationBuilder.DropColumn(
                name: "DeliveryCondition",
                table: "AllocateProductionOrders");

            migrationBuilder.DropColumn(
                name: "DeliveryEvidence",
                table: "AllocateProductionOrders");

            migrationBuilder.DropColumn(
                name: "DeliveryReceiver",
                table: "AllocateProductionOrders");

            migrationBuilder.DropColumn(
                name: "DispatchedAt",
                table: "AllocateProductionOrders");

            migrationBuilder.DropColumn(
                name: "DispatchedById",
                table: "AllocateProductionOrders");

            migrationBuilder.DropColumn(
                name: "ExitReference",
                table: "AllocateProductionOrders");

            migrationBuilder.DropColumn(
                name: "FefoExceptionReason",
                table: "AllocateProductionOrders");

            migrationBuilder.DropColumn(
                name: "LoadedDriver",
                table: "AllocateProductionOrders");

            migrationBuilder.DropColumn(
                name: "LoadedDriverPhone",
                table: "AllocateProductionOrders");

            migrationBuilder.DropColumn(
                name: "LoadedVehicle",
                table: "AllocateProductionOrders");

            migrationBuilder.DropColumn(
                name: "LoadingInspectionReference",
                table: "AllocateProductionOrders");

            migrationBuilder.DropColumn(
                name: "LoadingLocation",
                table: "AllocateProductionOrders");

            migrationBuilder.DropColumn(
                name: "MinimumRemainingShelfLifeDays",
                table: "AllocateProductionOrders");
        }
    }
}
