using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddStockMigrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StockMigrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    MassUnit = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    VolumeUnit = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedById = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMigrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockMigrations_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockMigrations_users_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockMigrations_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockMigrations_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockMigrations_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StockMigrationLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StockMigrationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sheet = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RowNumber = table.Column<int>(type: "integer", nullable: false),
                    SheetWarehouse = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SheetShelf = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SheetMaterialCode = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SheetMaterialName = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SheetBatchNumber = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SheetUnit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SheetQuantity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SheetManufacturingDate = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SheetExpiryDate = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Grn = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ArNumber = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    MaterialCode = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: true),
                    WarehouseName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ShelfName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ShelfId = table.Column<Guid>(type: "uuid", nullable: true),
                    BatchNumber = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BatchNumberDamaged = table.Column<bool>(type: "boolean", nullable: false),
                    UnitSymbol = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    QuantityInSheet = table.Column<decimal>(type: "numeric", nullable: true),
                    ManufacturingDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Excluded = table.Column<bool>(type: "boolean", nullable: false),
                    ResolvedMaterialId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResolvedShelfId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResolvedWarehouseId = table.Column<Guid>(type: "uuid", nullable: true),
                    UoMId = table.Column<Guid>(type: "uuid", nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Blockers = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Warnings = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Corrections = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    ImportedBatchId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMigrationLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockMigrationLines_MaterialBatches_ImportedBatchId",
                        column: x => x.ImportedBatchId,
                        principalTable: "MaterialBatches",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockMigrationLines_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockMigrationLines_StockMigrations_StockMigrationId",
                        column: x => x.StockMigrationId,
                        principalTable: "StockMigrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockMigrationLines_UnitOfMeasures_UoMId",
                        column: x => x.UoMId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockMigrationLines_WarehouseLocationShelves_ShelfId",
                        column: x => x.ShelfId,
                        principalTable: "WarehouseLocationShelves",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockMigrationLines_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockMigrationLines_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockMigrationLines_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StockMigrationWriteOffs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StockMigrationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShelfMaterialBatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialBatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    PreviousQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    PreviousBatchStatus = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMigrationWriteOffs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockMigrationWriteOffs_ShelfMaterialBatches_ShelfMaterialB~",
                        column: x => x.ShelfMaterialBatchId,
                        principalTable: "ShelfMaterialBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockMigrationWriteOffs_StockMigrations_StockMigrationId",
                        column: x => x.StockMigrationId,
                        principalTable: "StockMigrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockMigrationWriteOffs_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockMigrationWriteOffs_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockMigrationWriteOffs_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockMigrationLines_CreatedById",
                table: "StockMigrationLines",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_StockMigrationLines_ImportedBatchId",
                table: "StockMigrationLines",
                column: "ImportedBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMigrationLines_LastDeletedById",
                table: "StockMigrationLines",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_StockMigrationLines_LastUpdatedById",
                table: "StockMigrationLines",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_StockMigrationLines_MaterialId",
                table: "StockMigrationLines",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMigrationLines_ShelfId",
                table: "StockMigrationLines",
                column: "ShelfId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMigrationLines_StockMigrationId",
                table: "StockMigrationLines",
                column: "StockMigrationId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMigrationLines_UoMId",
                table: "StockMigrationLines",
                column: "UoMId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMigrations_ApprovedById",
                table: "StockMigrations",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_StockMigrations_CreatedById",
                table: "StockMigrations",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_StockMigrations_DepartmentId",
                table: "StockMigrations",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMigrations_LastDeletedById",
                table: "StockMigrations",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_StockMigrations_LastUpdatedById",
                table: "StockMigrations",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_StockMigrationWriteOffs_CreatedById",
                table: "StockMigrationWriteOffs",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_StockMigrationWriteOffs_LastDeletedById",
                table: "StockMigrationWriteOffs",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_StockMigrationWriteOffs_LastUpdatedById",
                table: "StockMigrationWriteOffs",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_StockMigrationWriteOffs_ShelfMaterialBatchId",
                table: "StockMigrationWriteOffs",
                column: "ShelfMaterialBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMigrationWriteOffs_StockMigrationId",
                table: "StockMigrationWriteOffs",
                column: "StockMigrationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockMigrationLines");

            migrationBuilder.DropTable(
                name: "StockMigrationWriteOffs");

            migrationBuilder.DropTable(
                name: "StockMigrations");
        }
    }
}
