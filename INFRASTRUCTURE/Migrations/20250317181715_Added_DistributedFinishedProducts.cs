using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class Added_DistributedFinishedProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DistributedFinishedProducts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseArrivalLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: true),
                    BatchManufacturingRecordId = table.Column<Guid>(type: "uuid", nullable: true),
                    UomId = table.Column<Guid>(type: "uuid", nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    DistributedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ArrivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_DistributedFinishedProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DistributedFinishedProducts_BatchManufacturingRecords_Batch~",
                        column: x => x.BatchManufacturingRecordId,
                        principalTable: "BatchManufacturingRecords",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DistributedFinishedProducts_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DistributedFinishedProducts_UnitOfMeasures_UomId",
                        column: x => x.UomId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DistributedFinishedProducts_WarehouseArrivalLocations_Wareh~",
                        column: x => x.WarehouseArrivalLocationId,
                        principalTable: "WarehouseArrivalLocations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DistributedFinishedProducts_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DistributedFinishedProducts_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DistributedFinishedProducts_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_DistributedFinishedProducts_BatchManufacturingRecordId",
                table: "DistributedFinishedProducts",
                column: "BatchManufacturingRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_DistributedFinishedProducts_CreatedById",
                table: "DistributedFinishedProducts",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_DistributedFinishedProducts_LastDeletedById",
                table: "DistributedFinishedProducts",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_DistributedFinishedProducts_LastUpdatedById",
                table: "DistributedFinishedProducts",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_DistributedFinishedProducts_ProductId",
                table: "DistributedFinishedProducts",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_DistributedFinishedProducts_UomId",
                table: "DistributedFinishedProducts",
                column: "UomId");

            migrationBuilder.CreateIndex(
                name: "IX_DistributedFinishedProducts_WarehouseArrivalLocationId",
                table: "DistributedFinishedProducts",
                column: "WarehouseArrivalLocationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DistributedFinishedProducts");
        }
    }
}
