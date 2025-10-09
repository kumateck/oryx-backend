using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddSwapRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SwapRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstWarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    SecondWarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ActionedById = table.Column<Guid>(type: "uuid", nullable: true),
                    ActionedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ActionNote = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SwapRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SwapRequests_Warehouses_FirstWarehouseId",
                        column: x => x.FirstWarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SwapRequests_Warehouses_SecondWarehouseId",
                        column: x => x.SecondWarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SwapRequests_users_ActionedById",
                        column: x => x.ActionedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SwapRequests_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SwapRequests_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SwapRequests_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SwapRequests_FirstSwapShelfMaterialBatches",
                columns: table => new
                {
                    SwapRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ShelfMaterialBatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialBatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    UoMId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SwapRequests_FirstSwapShelfMaterialBatches", x => new { x.SwapRequestId, x.Id });
                    table.ForeignKey(
                        name: "FK_SwapRequests_FirstSwapShelfMaterialBatches_MaterialBatches_~",
                        column: x => x.MaterialBatchId,
                        principalTable: "MaterialBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SwapRequests_FirstSwapShelfMaterialBatches_ShelfMaterialBat~",
                        column: x => x.ShelfMaterialBatchId,
                        principalTable: "ShelfMaterialBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SwapRequests_FirstSwapShelfMaterialBatches_SwapRequests_Swa~",
                        column: x => x.SwapRequestId,
                        principalTable: "SwapRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SwapRequests_FirstSwapShelfMaterialBatches_UnitOfMeasures_U~",
                        column: x => x.UoMId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SwapRequests_SecondSwapShelfMaterialBatches",
                columns: table => new
                {
                    SwapRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ShelfMaterialBatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialBatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    UoMId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SwapRequests_SecondSwapShelfMaterialBatches", x => new { x.SwapRequestId, x.Id });
                    table.ForeignKey(
                        name: "FK_SwapRequests_SecondSwapShelfMaterialBatches_MaterialBatches~",
                        column: x => x.MaterialBatchId,
                        principalTable: "MaterialBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SwapRequests_SecondSwapShelfMaterialBatches_ShelfMaterialBa~",
                        column: x => x.ShelfMaterialBatchId,
                        principalTable: "ShelfMaterialBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SwapRequests_SecondSwapShelfMaterialBatches_SwapRequests_Sw~",
                        column: x => x.SwapRequestId,
                        principalTable: "SwapRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SwapRequests_SecondSwapShelfMaterialBatches_UnitOfMeasures_~",
                        column: x => x.UoMId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SwapRequests_ActionedById",
                table: "SwapRequests",
                column: "ActionedById");

            migrationBuilder.CreateIndex(
                name: "IX_SwapRequests_CreatedById",
                table: "SwapRequests",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_SwapRequests_FirstWarehouseId",
                table: "SwapRequests",
                column: "FirstWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_SwapRequests_LastDeletedById",
                table: "SwapRequests",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_SwapRequests_LastUpdatedById",
                table: "SwapRequests",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_SwapRequests_SecondWarehouseId",
                table: "SwapRequests",
                column: "SecondWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_SwapRequests_FirstSwapShelfMaterialBatches_MaterialBatchId",
                table: "SwapRequests_FirstSwapShelfMaterialBatches",
                column: "MaterialBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_SwapRequests_FirstSwapShelfMaterialBatches_ShelfMaterialBat~",
                table: "SwapRequests_FirstSwapShelfMaterialBatches",
                column: "ShelfMaterialBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_SwapRequests_FirstSwapShelfMaterialBatches_UoMId",
                table: "SwapRequests_FirstSwapShelfMaterialBatches",
                column: "UoMId");

            migrationBuilder.CreateIndex(
                name: "IX_SwapRequests_SecondSwapShelfMaterialBatches_MaterialBatchId",
                table: "SwapRequests_SecondSwapShelfMaterialBatches",
                column: "MaterialBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_SwapRequests_SecondSwapShelfMaterialBatches_ShelfMaterialBa~",
                table: "SwapRequests_SecondSwapShelfMaterialBatches",
                column: "ShelfMaterialBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_SwapRequests_SecondSwapShelfMaterialBatches_UoMId",
                table: "SwapRequests_SecondSwapShelfMaterialBatches",
                column: "UoMId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SwapRequests_FirstSwapShelfMaterialBatches");

            migrationBuilder.DropTable(
                name: "SwapRequests_SecondSwapShelfMaterialBatches");

            migrationBuilder.DropTable(
                name: "SwapRequests");
        }
    }
}
