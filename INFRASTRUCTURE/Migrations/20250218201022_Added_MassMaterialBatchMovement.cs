using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class Added_MassMaterialBatchMovement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShelfMaterialBatch_MaterialBatches_MaterialBatchId",
                table: "ShelfMaterialBatch");

            migrationBuilder.DropForeignKey(
                name: "FK_ShelfMaterialBatch_WarehouseLocationShelves_WarehouseLocati~",
                table: "ShelfMaterialBatch");

            migrationBuilder.DropForeignKey(
                name: "FK_ShelfMaterialBatch_users_CreatedById",
                table: "ShelfMaterialBatch");

            migrationBuilder.DropForeignKey(
                name: "FK_ShelfMaterialBatch_users_LastDeletedById",
                table: "ShelfMaterialBatch");

            migrationBuilder.DropForeignKey(
                name: "FK_ShelfMaterialBatch_users_LastUpdatedById",
                table: "ShelfMaterialBatch");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ShelfMaterialBatch",
                table: "ShelfMaterialBatch");

            migrationBuilder.RenameTable(
                name: "ShelfMaterialBatch",
                newName: "ShelfMaterialBatches");

            migrationBuilder.RenameIndex(
                name: "IX_ShelfMaterialBatch_WarehouseLocationShelfId",
                table: "ShelfMaterialBatches",
                newName: "IX_ShelfMaterialBatches_WarehouseLocationShelfId");

            migrationBuilder.RenameIndex(
                name: "IX_ShelfMaterialBatch_MaterialBatchId",
                table: "ShelfMaterialBatches",
                newName: "IX_ShelfMaterialBatches_MaterialBatchId");

            migrationBuilder.RenameIndex(
                name: "IX_ShelfMaterialBatch_LastUpdatedById",
                table: "ShelfMaterialBatches",
                newName: "IX_ShelfMaterialBatches_LastUpdatedById");

            migrationBuilder.RenameIndex(
                name: "IX_ShelfMaterialBatch_LastDeletedById",
                table: "ShelfMaterialBatches",
                newName: "IX_ShelfMaterialBatches_LastDeletedById");

            migrationBuilder.RenameIndex(
                name: "IX_ShelfMaterialBatch_CreatedById",
                table: "ShelfMaterialBatches",
                newName: "IX_ShelfMaterialBatches_CreatedById");

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "ShelfMaterialBatches",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UomId",
                table: "ShelfMaterialBatches",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ShelfMaterialBatches",
                table: "ShelfMaterialBatches",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "MassMaterialBatchMovements",
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
                    table.PrimaryKey("PK_MassMaterialBatchMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MassMaterialBatchMovements_MaterialBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "MaterialBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MassMaterialBatchMovements_Warehouses_FromWarehouseId",
                        column: x => x.FromWarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MassMaterialBatchMovements_Warehouses_ToWarehouseId",
                        column: x => x.ToWarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MassMaterialBatchMovements_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MassMaterialBatchMovements_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MassMaterialBatchMovements_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MassMaterialBatchMovements_users_MovedById",
                        column: x => x.MovedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShelfMaterialBatches_UomId",
                table: "ShelfMaterialBatches",
                column: "UomId");

            migrationBuilder.CreateIndex(
                name: "IX_MassMaterialBatchMovements_BatchId",
                table: "MassMaterialBatchMovements",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_MassMaterialBatchMovements_CreatedById",
                table: "MassMaterialBatchMovements",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_MassMaterialBatchMovements_FromWarehouseId",
                table: "MassMaterialBatchMovements",
                column: "FromWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_MassMaterialBatchMovements_LastDeletedById",
                table: "MassMaterialBatchMovements",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_MassMaterialBatchMovements_LastUpdatedById",
                table: "MassMaterialBatchMovements",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_MassMaterialBatchMovements_MovedById",
                table: "MassMaterialBatchMovements",
                column: "MovedById");

            migrationBuilder.CreateIndex(
                name: "IX_MassMaterialBatchMovements_ToWarehouseId",
                table: "MassMaterialBatchMovements",
                column: "ToWarehouseId");

            migrationBuilder.AddForeignKey(
                name: "FK_ShelfMaterialBatches_MaterialBatches_MaterialBatchId",
                table: "ShelfMaterialBatches",
                column: "MaterialBatchId",
                principalTable: "MaterialBatches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ShelfMaterialBatches_UnitOfMeasures_UomId",
                table: "ShelfMaterialBatches",
                column: "UomId",
                principalTable: "UnitOfMeasures",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ShelfMaterialBatches_WarehouseLocationShelves_WarehouseLoca~",
                table: "ShelfMaterialBatches",
                column: "WarehouseLocationShelfId",
                principalTable: "WarehouseLocationShelves",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ShelfMaterialBatches_users_CreatedById",
                table: "ShelfMaterialBatches",
                column: "CreatedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ShelfMaterialBatches_users_LastDeletedById",
                table: "ShelfMaterialBatches",
                column: "LastDeletedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ShelfMaterialBatches_users_LastUpdatedById",
                table: "ShelfMaterialBatches",
                column: "LastUpdatedById",
                principalTable: "users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShelfMaterialBatches_MaterialBatches_MaterialBatchId",
                table: "ShelfMaterialBatches");

            migrationBuilder.DropForeignKey(
                name: "FK_ShelfMaterialBatches_UnitOfMeasures_UomId",
                table: "ShelfMaterialBatches");

            migrationBuilder.DropForeignKey(
                name: "FK_ShelfMaterialBatches_WarehouseLocationShelves_WarehouseLoca~",
                table: "ShelfMaterialBatches");

            migrationBuilder.DropForeignKey(
                name: "FK_ShelfMaterialBatches_users_CreatedById",
                table: "ShelfMaterialBatches");

            migrationBuilder.DropForeignKey(
                name: "FK_ShelfMaterialBatches_users_LastDeletedById",
                table: "ShelfMaterialBatches");

            migrationBuilder.DropForeignKey(
                name: "FK_ShelfMaterialBatches_users_LastUpdatedById",
                table: "ShelfMaterialBatches");

            migrationBuilder.DropTable(
                name: "MassMaterialBatchMovements");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ShelfMaterialBatches",
                table: "ShelfMaterialBatches");

            migrationBuilder.DropIndex(
                name: "IX_ShelfMaterialBatches_UomId",
                table: "ShelfMaterialBatches");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "ShelfMaterialBatches");

            migrationBuilder.DropColumn(
                name: "UomId",
                table: "ShelfMaterialBatches");

            migrationBuilder.RenameTable(
                name: "ShelfMaterialBatches",
                newName: "ShelfMaterialBatch");

            migrationBuilder.RenameIndex(
                name: "IX_ShelfMaterialBatches_WarehouseLocationShelfId",
                table: "ShelfMaterialBatch",
                newName: "IX_ShelfMaterialBatch_WarehouseLocationShelfId");

            migrationBuilder.RenameIndex(
                name: "IX_ShelfMaterialBatches_MaterialBatchId",
                table: "ShelfMaterialBatch",
                newName: "IX_ShelfMaterialBatch_MaterialBatchId");

            migrationBuilder.RenameIndex(
                name: "IX_ShelfMaterialBatches_LastUpdatedById",
                table: "ShelfMaterialBatch",
                newName: "IX_ShelfMaterialBatch_LastUpdatedById");

            migrationBuilder.RenameIndex(
                name: "IX_ShelfMaterialBatches_LastDeletedById",
                table: "ShelfMaterialBatch",
                newName: "IX_ShelfMaterialBatch_LastDeletedById");

            migrationBuilder.RenameIndex(
                name: "IX_ShelfMaterialBatches_CreatedById",
                table: "ShelfMaterialBatch",
                newName: "IX_ShelfMaterialBatch_CreatedById");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ShelfMaterialBatch",
                table: "ShelfMaterialBatch",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ShelfMaterialBatch_MaterialBatches_MaterialBatchId",
                table: "ShelfMaterialBatch",
                column: "MaterialBatchId",
                principalTable: "MaterialBatches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ShelfMaterialBatch_WarehouseLocationShelves_WarehouseLocati~",
                table: "ShelfMaterialBatch",
                column: "WarehouseLocationShelfId",
                principalTable: "WarehouseLocationShelves",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ShelfMaterialBatch_users_CreatedById",
                table: "ShelfMaterialBatch",
                column: "CreatedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ShelfMaterialBatch_users_LastDeletedById",
                table: "ShelfMaterialBatch",
                column: "LastDeletedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ShelfMaterialBatch_users_LastUpdatedById",
                table: "ShelfMaterialBatch",
                column: "LastUpdatedById",
                principalTable: "users",
                principalColumn: "Id");
        }
    }
}
