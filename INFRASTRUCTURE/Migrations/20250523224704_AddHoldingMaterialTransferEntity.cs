using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddHoldingMaterialTransferEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GHoldingMaterialTransfers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ModelType = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_GHoldingMaterialTransfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GHoldingMaterialTransfers_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GHoldingMaterialTransfers_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GHoldingMaterialTransfers_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "HoldingMaterialTransferBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HoldingMaterialTransferId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialBatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    UoMId = table.Column<Guid>(type: "uuid", nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    SourceWarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    DestinationWarehouseId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HoldingMaterialTransferBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HoldingMaterialTransferBatches_GHoldingMaterialTransfers_Ho~",
                        column: x => x.HoldingMaterialTransferId,
                        principalTable: "GHoldingMaterialTransfers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HoldingMaterialTransferBatches_MaterialBatches_MaterialBatc~",
                        column: x => x.MaterialBatchId,
                        principalTable: "MaterialBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HoldingMaterialTransferBatches_UnitOfMeasures_UoMId",
                        column: x => x.UoMId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_HoldingMaterialTransferBatches_Warehouses_DestinationWareho~",
                        column: x => x.DestinationWarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HoldingMaterialTransferBatches_Warehouses_SourceWarehouseId",
                        column: x => x.SourceWarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GHoldingMaterialTransfers_CreatedById",
                table: "GHoldingMaterialTransfers",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_GHoldingMaterialTransfers_LastDeletedById",
                table: "GHoldingMaterialTransfers",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_GHoldingMaterialTransfers_LastUpdatedById",
                table: "GHoldingMaterialTransfers",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_HoldingMaterialTransferBatches_DestinationWarehouseId",
                table: "HoldingMaterialTransferBatches",
                column: "DestinationWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_HoldingMaterialTransferBatches_HoldingMaterialTransferId",
                table: "HoldingMaterialTransferBatches",
                column: "HoldingMaterialTransferId");

            migrationBuilder.CreateIndex(
                name: "IX_HoldingMaterialTransferBatches_MaterialBatchId",
                table: "HoldingMaterialTransferBatches",
                column: "MaterialBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_HoldingMaterialTransferBatches_SourceWarehouseId",
                table: "HoldingMaterialTransferBatches",
                column: "SourceWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_HoldingMaterialTransferBatches_UoMId",
                table: "HoldingMaterialTransferBatches",
                column: "UoMId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HoldingMaterialTransferBatches");

            migrationBuilder.DropTable(
                name: "GHoldingMaterialTransfers");
        }
    }
}
