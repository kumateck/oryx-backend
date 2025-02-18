using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class Added_Sr_Table : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sr_MaterialBatches_MaterialBatchId",
                table: "Sr");

            migrationBuilder.DropForeignKey(
                name: "FK_Sr_UnitOfMeasures_UoMId",
                table: "Sr");

            migrationBuilder.DropForeignKey(
                name: "FK_Sr_users_CreatedById",
                table: "Sr");

            migrationBuilder.DropForeignKey(
                name: "FK_Sr_users_LastDeletedById",
                table: "Sr");

            migrationBuilder.DropForeignKey(
                name: "FK_Sr_users_LastUpdatedById",
                table: "Sr");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Sr",
                table: "Sr");

            migrationBuilder.RenameTable(
                name: "Sr",
                newName: "Srs");

            migrationBuilder.RenameIndex(
                name: "IX_Sr_UoMId",
                table: "Srs",
                newName: "IX_Srs_UoMId");

            migrationBuilder.RenameIndex(
                name: "IX_Sr_MaterialBatchId",
                table: "Srs",
                newName: "IX_Srs_MaterialBatchId");

            migrationBuilder.RenameIndex(
                name: "IX_Sr_LastUpdatedById",
                table: "Srs",
                newName: "IX_Srs_LastUpdatedById");

            migrationBuilder.RenameIndex(
                name: "IX_Sr_LastDeletedById",
                table: "Srs",
                newName: "IX_Srs_LastDeletedById");

            migrationBuilder.RenameIndex(
                name: "IX_Sr_CreatedById",
                table: "Srs",
                newName: "IX_Srs_CreatedById");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Srs",
                table: "Srs",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "ShelfMaterialBatch",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseLocationShelfId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialBatchId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_ShelfMaterialBatch", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShelfMaterialBatch_MaterialBatches_MaterialBatchId",
                        column: x => x.MaterialBatchId,
                        principalTable: "MaterialBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShelfMaterialBatch_WarehouseLocationShelves_WarehouseLocati~",
                        column: x => x.WarehouseLocationShelfId,
                        principalTable: "WarehouseLocationShelves",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShelfMaterialBatch_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ShelfMaterialBatch_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ShelfMaterialBatch_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShelfMaterialBatch_CreatedById",
                table: "ShelfMaterialBatch",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ShelfMaterialBatch_LastDeletedById",
                table: "ShelfMaterialBatch",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ShelfMaterialBatch_LastUpdatedById",
                table: "ShelfMaterialBatch",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ShelfMaterialBatch_MaterialBatchId",
                table: "ShelfMaterialBatch",
                column: "MaterialBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_ShelfMaterialBatch_WarehouseLocationShelfId",
                table: "ShelfMaterialBatch",
                column: "WarehouseLocationShelfId");

            migrationBuilder.AddForeignKey(
                name: "FK_Srs_MaterialBatches_MaterialBatchId",
                table: "Srs",
                column: "MaterialBatchId",
                principalTable: "MaterialBatches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Srs_UnitOfMeasures_UoMId",
                table: "Srs",
                column: "UoMId",
                principalTable: "UnitOfMeasures",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Srs_users_CreatedById",
                table: "Srs",
                column: "CreatedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Srs_users_LastDeletedById",
                table: "Srs",
                column: "LastDeletedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Srs_users_LastUpdatedById",
                table: "Srs",
                column: "LastUpdatedById",
                principalTable: "users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Srs_MaterialBatches_MaterialBatchId",
                table: "Srs");

            migrationBuilder.DropForeignKey(
                name: "FK_Srs_UnitOfMeasures_UoMId",
                table: "Srs");

            migrationBuilder.DropForeignKey(
                name: "FK_Srs_users_CreatedById",
                table: "Srs");

            migrationBuilder.DropForeignKey(
                name: "FK_Srs_users_LastDeletedById",
                table: "Srs");

            migrationBuilder.DropForeignKey(
                name: "FK_Srs_users_LastUpdatedById",
                table: "Srs");

            migrationBuilder.DropTable(
                name: "ShelfMaterialBatch");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Srs",
                table: "Srs");

            migrationBuilder.RenameTable(
                name: "Srs",
                newName: "Sr");

            migrationBuilder.RenameIndex(
                name: "IX_Srs_UoMId",
                table: "Sr",
                newName: "IX_Sr_UoMId");

            migrationBuilder.RenameIndex(
                name: "IX_Srs_MaterialBatchId",
                table: "Sr",
                newName: "IX_Sr_MaterialBatchId");

            migrationBuilder.RenameIndex(
                name: "IX_Srs_LastUpdatedById",
                table: "Sr",
                newName: "IX_Sr_LastUpdatedById");

            migrationBuilder.RenameIndex(
                name: "IX_Srs_LastDeletedById",
                table: "Sr",
                newName: "IX_Sr_LastDeletedById");

            migrationBuilder.RenameIndex(
                name: "IX_Srs_CreatedById",
                table: "Sr",
                newName: "IX_Sr_CreatedById");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Sr",
                table: "Sr",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Sr_MaterialBatches_MaterialBatchId",
                table: "Sr",
                column: "MaterialBatchId",
                principalTable: "MaterialBatches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Sr_UnitOfMeasures_UoMId",
                table: "Sr",
                column: "UoMId",
                principalTable: "UnitOfMeasures",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Sr_users_CreatedById",
                table: "Sr",
                column: "CreatedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Sr_users_LastDeletedById",
                table: "Sr",
                column: "LastDeletedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Sr_users_LastUpdatedById",
                table: "Sr",
                column: "LastUpdatedById",
                principalTable: "users",
                principalColumn: "Id");
        }
    }
}
