using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class ChangeDistributedRequisitionMaterial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DistributedRequisitionMaterials_UnitOfMeasures_UomId",
                table: "DistributedRequisitionMaterials");

            migrationBuilder.RenameColumn(
                name: "UomId",
                table: "DistributedRequisitionMaterials",
                newName: "UoMId");

            migrationBuilder.RenameIndex(
                name: "IX_DistributedRequisitionMaterials_UomId",
                table: "DistributedRequisitionMaterials",
                newName: "IX_DistributedRequisitionMaterials_UoMId");

            migrationBuilder.CreateTable(
                name: "DistributedRequisitionItem",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequisitionItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    DistributedRequisitionMaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    UoMId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DistributedRequisitionItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DistributedRequisitionItem_DistributedRequisitionMaterials_~",
                        column: x => x.DistributedRequisitionMaterialId,
                        principalTable: "DistributedRequisitionMaterials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DistributedRequisitionItem_RequisitionItems_RequisitionItem~",
                        column: x => x.RequisitionItemId,
                        principalTable: "RequisitionItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DistributedRequisitionItem_UnitOfMeasures_UoMId",
                        column: x => x.UoMId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DistributedRequisitionItem_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DistributeMaterials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialBatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    UoMId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    DistributedRequisitionMaterialId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_DistributeMaterials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DistributeMaterials_DistributedRequisitionMaterials_Distrib~",
                        column: x => x.DistributedRequisitionMaterialId,
                        principalTable: "DistributedRequisitionMaterials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DistributeMaterials_MaterialBatches_MaterialBatchId",
                        column: x => x.MaterialBatchId,
                        principalTable: "MaterialBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DistributeMaterials_UnitOfMeasures_UoMId",
                        column: x => x.UoMId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DistributeMaterials_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DistributeMaterials_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DistributeMaterials_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DistributeMaterials_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_DistributedRequisitionItem_DistributedRequisitionMaterialId",
                table: "DistributedRequisitionItem",
                column: "DistributedRequisitionMaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_DistributedRequisitionItem_RequisitionItemId",
                table: "DistributedRequisitionItem",
                column: "RequisitionItemId");

            migrationBuilder.CreateIndex(
                name: "IX_DistributedRequisitionItem_UoMId",
                table: "DistributedRequisitionItem",
                column: "UoMId");

            migrationBuilder.CreateIndex(
                name: "IX_DistributedRequisitionItem_WarehouseId",
                table: "DistributedRequisitionItem",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_DistributeMaterials_CreatedById",
                table: "DistributeMaterials",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_DistributeMaterials_DistributedRequisitionMaterialId",
                table: "DistributeMaterials",
                column: "DistributedRequisitionMaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_DistributeMaterials_LastDeletedById",
                table: "DistributeMaterials",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_DistributeMaterials_LastUpdatedById",
                table: "DistributeMaterials",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_DistributeMaterials_MaterialBatchId",
                table: "DistributeMaterials",
                column: "MaterialBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_DistributeMaterials_UoMId",
                table: "DistributeMaterials",
                column: "UoMId");

            migrationBuilder.CreateIndex(
                name: "IX_DistributeMaterials_WarehouseId",
                table: "DistributeMaterials",
                column: "WarehouseId");

            migrationBuilder.AddForeignKey(
                name: "FK_DistributedRequisitionMaterials_UnitOfMeasures_UoMId",
                table: "DistributedRequisitionMaterials",
                column: "UoMId",
                principalTable: "UnitOfMeasures",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DistributedRequisitionMaterials_UnitOfMeasures_UoMId",
                table: "DistributedRequisitionMaterials");

            migrationBuilder.DropTable(
                name: "DistributedRequisitionItem");

            migrationBuilder.DropTable(
                name: "DistributeMaterials");

            migrationBuilder.RenameColumn(
                name: "UoMId",
                table: "DistributedRequisitionMaterials",
                newName: "UomId");

            migrationBuilder.RenameIndex(
                name: "IX_DistributedRequisitionMaterials_UoMId",
                table: "DistributedRequisitionMaterials",
                newName: "IX_DistributedRequisitionMaterials_UomId");

            migrationBuilder.AddForeignKey(
                name: "FK_DistributedRequisitionMaterials_UnitOfMeasures_UomId",
                table: "DistributedRequisitionMaterials",
                column: "UomId",
                principalTable: "UnitOfMeasures",
                principalColumn: "Id");
        }
    }
}
