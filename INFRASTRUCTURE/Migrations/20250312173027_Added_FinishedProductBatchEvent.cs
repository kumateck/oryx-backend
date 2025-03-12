using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class Added_FinishedProductBatchEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FinishedGoodsTransferNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FromWarehouseId = table.Column<Guid>(type: "uuid", nullable: true),
                    ToWarehouseId = table.Column<Guid>(type: "uuid", nullable: true),
                    QuantityPerPack = table.Column<decimal>(type: "numeric", nullable: false),
                    PackageStyleId = table.Column<Guid>(type: "uuid", nullable: true),
                    UoMId = table.Column<Guid>(type: "uuid", nullable: true),
                    TotalQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    QarNumber = table.Column<string>(type: "text", nullable: true),
                    BatchManufacturingRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinishedGoodsTransferNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinishedGoodsTransferNotes_BatchManufacturingRecords_BatchM~",
                        column: x => x.BatchManufacturingRecordId,
                        principalTable: "BatchManufacturingRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FinishedGoodsTransferNotes_PackageStyles_PackageStyleId",
                        column: x => x.PackageStyleId,
                        principalTable: "PackageStyles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FinishedGoodsTransferNotes_UnitOfMeasures_UoMId",
                        column: x => x.UoMId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FinishedGoodsTransferNotes_Warehouses_FromWarehouseId",
                        column: x => x.FromWarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FinishedGoodsTransferNotes_Warehouses_ToWarehouseId",
                        column: x => x.ToWarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FinishedGoodsTransferNotes_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FinishedGoodsTransferNotes_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FinishedGoodsTransferNotes_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FinishedProductBatchEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    ConsumptionWarehouseId = table.Column<Guid>(type: "uuid", nullable: true),
                    ConsumedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinishedProductBatchEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinishedProductBatchEvents_Products_BatchId",
                        column: x => x.BatchId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FinishedProductBatchEvents_Warehouses_ConsumptionWarehouseId",
                        column: x => x.ConsumptionWarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FinishedProductBatchEvents_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FinishedProductBatchEvents_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FinishedProductBatchEvents_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FinishedProductBatchEvents_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FinishedProductBatchMovements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromWarehouseId = table.Column<Guid>(type: "uuid", nullable: true),
                    ToWarehouseId = table.Column<Guid>(type: "uuid", nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    MovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MovedById = table.Column<Guid>(type: "uuid", nullable: false),
                    MovementType = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinishedProductBatchMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinishedProductBatchMovements_Products_BatchId",
                        column: x => x.BatchId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FinishedProductBatchMovements_Warehouses_FromWarehouseId",
                        column: x => x.FromWarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FinishedProductBatchMovements_Warehouses_ToWarehouseId",
                        column: x => x.ToWarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FinishedProductBatchMovements_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FinishedProductBatchMovements_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FinishedProductBatchMovements_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FinishedProductBatchMovements_users_MovedById",
                        column: x => x.MovedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FinishedGoodsTransferNotes_BatchManufacturingRecordId",
                table: "FinishedGoodsTransferNotes",
                column: "BatchManufacturingRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_FinishedGoodsTransferNotes_CreatedById",
                table: "FinishedGoodsTransferNotes",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_FinishedGoodsTransferNotes_FromWarehouseId",
                table: "FinishedGoodsTransferNotes",
                column: "FromWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_FinishedGoodsTransferNotes_LastDeletedById",
                table: "FinishedGoodsTransferNotes",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_FinishedGoodsTransferNotes_LastUpdatedById",
                table: "FinishedGoodsTransferNotes",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_FinishedGoodsTransferNotes_PackageStyleId",
                table: "FinishedGoodsTransferNotes",
                column: "PackageStyleId");

            migrationBuilder.CreateIndex(
                name: "IX_FinishedGoodsTransferNotes_ToWarehouseId",
                table: "FinishedGoodsTransferNotes",
                column: "ToWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_FinishedGoodsTransferNotes_UoMId",
                table: "FinishedGoodsTransferNotes",
                column: "UoMId");

            migrationBuilder.CreateIndex(
                name: "IX_FinishedProductBatchEvents_BatchId",
                table: "FinishedProductBatchEvents",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_FinishedProductBatchEvents_ConsumptionWarehouseId",
                table: "FinishedProductBatchEvents",
                column: "ConsumptionWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_FinishedProductBatchEvents_CreatedById",
                table: "FinishedProductBatchEvents",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_FinishedProductBatchEvents_LastDeletedById",
                table: "FinishedProductBatchEvents",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_FinishedProductBatchEvents_LastUpdatedById",
                table: "FinishedProductBatchEvents",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_FinishedProductBatchEvents_UserId",
                table: "FinishedProductBatchEvents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FinishedProductBatchMovements_BatchId",
                table: "FinishedProductBatchMovements",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_FinishedProductBatchMovements_CreatedById",
                table: "FinishedProductBatchMovements",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_FinishedProductBatchMovements_FromWarehouseId",
                table: "FinishedProductBatchMovements",
                column: "FromWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_FinishedProductBatchMovements_LastDeletedById",
                table: "FinishedProductBatchMovements",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_FinishedProductBatchMovements_LastUpdatedById",
                table: "FinishedProductBatchMovements",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_FinishedProductBatchMovements_MovedById",
                table: "FinishedProductBatchMovements",
                column: "MovedById");

            migrationBuilder.CreateIndex(
                name: "IX_FinishedProductBatchMovements_ToWarehouseId",
                table: "FinishedProductBatchMovements",
                column: "ToWarehouseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FinishedGoodsTransferNotes");

            migrationBuilder.DropTable(
                name: "FinishedProductBatchEvents");

            migrationBuilder.DropTable(
                name: "FinishedProductBatchMovements");
        }
    }
}
