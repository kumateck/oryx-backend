using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialReturn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ApprovedById",
                table: "RequisitionApprovals",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ApprovedById",
                table: "PurchaseOrderApprovals",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UoMId",
                table: "MaterialBatchReservedQuantities",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ApprovedById",
                table: "BillingSheetApprovals",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MaterialReturnNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReturnDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProductionScheduleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
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
                    table.PrimaryKey("PK_MaterialReturnNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialReturnNotes_ProductionSchedules_ProductionScheduleId",
                        column: x => x.ProductionScheduleId,
                        principalTable: "ProductionSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MaterialReturnNotes_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MaterialReturnNotes_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaterialReturnNotes_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaterialReturnNotes_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MaterialReturnNoteFullReturns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialReturnNoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialBatchReservedQuantityId = table.Column<Guid>(type: "uuid", nullable: false),
                    DestinationWarehouseId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialReturnNoteFullReturns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialReturnNoteFullReturns_MaterialBatchReservedQuantiti~",
                        column: x => x.MaterialBatchReservedQuantityId,
                        principalTable: "MaterialBatchReservedQuantities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MaterialReturnNoteFullReturns_MaterialReturnNotes_MaterialR~",
                        column: x => x.MaterialReturnNoteId,
                        principalTable: "MaterialReturnNotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MaterialReturnNoteFullReturns_Warehouses_DestinationWarehou~",
                        column: x => x.DestinationWarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MaterialReturnNotePartialReturns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialReturnNoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    UoMId = table.Column<Guid>(type: "uuid", nullable: true),
                    DestinationWarehouseId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialReturnNotePartialReturns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialReturnNotePartialReturns_MaterialReturnNotes_Materi~",
                        column: x => x.MaterialReturnNoteId,
                        principalTable: "MaterialReturnNotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MaterialReturnNotePartialReturns_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MaterialReturnNotePartialReturns_UnitOfMeasures_UoMId",
                        column: x => x.UoMId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaterialReturnNotePartialReturns_Warehouses_DestinationWare~",
                        column: x => x.DestinationWarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RequisitionApprovals_ApprovedById",
                table: "RequisitionApprovals",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderApprovals_ApprovedById",
                table: "PurchaseOrderApprovals",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialBatchReservedQuantities_UoMId",
                table: "MaterialBatchReservedQuantities",
                column: "UoMId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingSheetApprovals_ApprovedById",
                table: "BillingSheetApprovals",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialReturnNoteFullReturns_DestinationWarehouseId",
                table: "MaterialReturnNoteFullReturns",
                column: "DestinationWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialReturnNoteFullReturns_MaterialBatchReservedQuantity~",
                table: "MaterialReturnNoteFullReturns",
                column: "MaterialBatchReservedQuantityId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialReturnNoteFullReturns_MaterialReturnNoteId",
                table: "MaterialReturnNoteFullReturns",
                column: "MaterialReturnNoteId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialReturnNotePartialReturns_DestinationWarehouseId",
                table: "MaterialReturnNotePartialReturns",
                column: "DestinationWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialReturnNotePartialReturns_MaterialId",
                table: "MaterialReturnNotePartialReturns",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialReturnNotePartialReturns_MaterialReturnNoteId",
                table: "MaterialReturnNotePartialReturns",
                column: "MaterialReturnNoteId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialReturnNotePartialReturns_UoMId",
                table: "MaterialReturnNotePartialReturns",
                column: "UoMId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialReturnNotes_CreatedById",
                table: "MaterialReturnNotes",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialReturnNotes_LastDeletedById",
                table: "MaterialReturnNotes",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialReturnNotes_LastUpdatedById",
                table: "MaterialReturnNotes",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialReturnNotes_ProductId",
                table: "MaterialReturnNotes",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialReturnNotes_ProductionScheduleId",
                table: "MaterialReturnNotes",
                column: "ProductionScheduleId");

            migrationBuilder.AddForeignKey(
                name: "FK_BillingSheetApprovals_users_ApprovedById",
                table: "BillingSheetApprovals",
                column: "ApprovedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialBatchReservedQuantities_UnitOfMeasures_UoMId",
                table: "MaterialBatchReservedQuantities",
                column: "UoMId",
                principalTable: "UnitOfMeasures",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrderApprovals_users_ApprovedById",
                table: "PurchaseOrderApprovals",
                column: "ApprovedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RequisitionApprovals_users_ApprovedById",
                table: "RequisitionApprovals",
                column: "ApprovedById",
                principalTable: "users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BillingSheetApprovals_users_ApprovedById",
                table: "BillingSheetApprovals");

            migrationBuilder.DropForeignKey(
                name: "FK_MaterialBatchReservedQuantities_UnitOfMeasures_UoMId",
                table: "MaterialBatchReservedQuantities");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrderApprovals_users_ApprovedById",
                table: "PurchaseOrderApprovals");

            migrationBuilder.DropForeignKey(
                name: "FK_RequisitionApprovals_users_ApprovedById",
                table: "RequisitionApprovals");

            migrationBuilder.DropTable(
                name: "MaterialReturnNoteFullReturns");

            migrationBuilder.DropTable(
                name: "MaterialReturnNotePartialReturns");

            migrationBuilder.DropTable(
                name: "MaterialReturnNotes");

            migrationBuilder.DropIndex(
                name: "IX_RequisitionApprovals_ApprovedById",
                table: "RequisitionApprovals");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrderApprovals_ApprovedById",
                table: "PurchaseOrderApprovals");

            migrationBuilder.DropIndex(
                name: "IX_MaterialBatchReservedQuantities_UoMId",
                table: "MaterialBatchReservedQuantities");

            migrationBuilder.DropIndex(
                name: "IX_BillingSheetApprovals_ApprovedById",
                table: "BillingSheetApprovals");

            migrationBuilder.DropColumn(
                name: "ApprovedById",
                table: "RequisitionApprovals");

            migrationBuilder.DropColumn(
                name: "ApprovedById",
                table: "PurchaseOrderApprovals");

            migrationBuilder.DropColumn(
                name: "UoMId",
                table: "MaterialBatchReservedQuantities");

            migrationBuilder.DropColumn(
                name: "ApprovedById",
                table: "BillingSheetApprovals");
        }
    }
}
