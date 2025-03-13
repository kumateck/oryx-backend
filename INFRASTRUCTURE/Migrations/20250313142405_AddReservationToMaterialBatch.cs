using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddReservationToMaterialBatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MaterialBatchReservedQuantities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialBatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionScheduleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_MaterialBatchReservedQuantities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialBatchReservedQuantities_MaterialBatches_MaterialBat~",
                        column: x => x.MaterialBatchId,
                        principalTable: "MaterialBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MaterialBatchReservedQuantities_ProductionSchedules_Product~",
                        column: x => x.ProductionScheduleId,
                        principalTable: "ProductionSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MaterialBatchReservedQuantities_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MaterialBatchReservedQuantities_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MaterialBatchReservedQuantities_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaterialBatchReservedQuantities_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaterialBatchReservedQuantities_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_MaterialBatchReservedQuantities_CreatedById",
                table: "MaterialBatchReservedQuantities",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialBatchReservedQuantities_LastDeletedById",
                table: "MaterialBatchReservedQuantities",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialBatchReservedQuantities_LastUpdatedById",
                table: "MaterialBatchReservedQuantities",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialBatchReservedQuantities_MaterialBatchId",
                table: "MaterialBatchReservedQuantities",
                column: "MaterialBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialBatchReservedQuantities_ProductId",
                table: "MaterialBatchReservedQuantities",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialBatchReservedQuantities_ProductionScheduleId",
                table: "MaterialBatchReservedQuantities",
                column: "ProductionScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialBatchReservedQuantities_WarehouseId",
                table: "MaterialBatchReservedQuantities",
                column: "WarehouseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MaterialBatchReservedQuantities");
        }
    }
}
