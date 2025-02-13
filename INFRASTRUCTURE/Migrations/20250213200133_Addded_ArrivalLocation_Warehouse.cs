using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class Addded_ArrivalLocation_Warehouse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WarehouseArrivalLocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    FloorName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseArrivalLocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehouseArrivalLocations_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WarehouseArrivalLocations_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WarehouseArrivalLocations_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WarehouseArrivalLocations_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DistributedRequisitionMaterials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequisitionItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseArrivalLocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    UomId = table.Column<Guid>(type: "uuid", nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    ConfirmArrival = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DistributedRequisitionMaterials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DistributedRequisitionMaterials_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DistributedRequisitionMaterials_RequisitionItems_Requisitio~",
                        column: x => x.RequisitionItemId,
                        principalTable: "RequisitionItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DistributedRequisitionMaterials_UnitOfMeasures_UomId",
                        column: x => x.UomId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DistributedRequisitionMaterials_WarehouseArrivalLocations_W~",
                        column: x => x.WarehouseArrivalLocationId,
                        principalTable: "WarehouseArrivalLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DistributedRequisitionMaterials_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DistributedRequisitionMaterials_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DistributedRequisitionMaterials_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_DistributedRequisitionMaterials_CreatedById",
                table: "DistributedRequisitionMaterials",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_DistributedRequisitionMaterials_LastDeletedById",
                table: "DistributedRequisitionMaterials",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_DistributedRequisitionMaterials_LastUpdatedById",
                table: "DistributedRequisitionMaterials",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_DistributedRequisitionMaterials_MaterialId",
                table: "DistributedRequisitionMaterials",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_DistributedRequisitionMaterials_RequisitionItemId",
                table: "DistributedRequisitionMaterials",
                column: "RequisitionItemId");

            migrationBuilder.CreateIndex(
                name: "IX_DistributedRequisitionMaterials_UomId",
                table: "DistributedRequisitionMaterials",
                column: "UomId");

            migrationBuilder.CreateIndex(
                name: "IX_DistributedRequisitionMaterials_WarehouseArrivalLocationId",
                table: "DistributedRequisitionMaterials",
                column: "WarehouseArrivalLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseArrivalLocations_CreatedById",
                table: "WarehouseArrivalLocations",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseArrivalLocations_LastDeletedById",
                table: "WarehouseArrivalLocations",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseArrivalLocations_LastUpdatedById",
                table: "WarehouseArrivalLocations",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseArrivalLocations_WarehouseId",
                table: "WarehouseArrivalLocations",
                column: "WarehouseId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DistributedRequisitionMaterials");

            migrationBuilder.DropTable(
                name: "WarehouseArrivalLocations");
        }
    }
}
