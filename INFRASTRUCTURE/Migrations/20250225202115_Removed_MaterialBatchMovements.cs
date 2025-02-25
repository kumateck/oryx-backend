using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class Removed_MaterialBatchMovements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MaterialBatchMovements");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MaterialBatchMovements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    FromLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    MovedById = table.Column<Guid>(type: "uuid", nullable: false),
                    ToLocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MovementType = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialBatchMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialBatchMovements_MaterialBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "MaterialBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MaterialBatchMovements_WarehouseLocations_FromLocationId",
                        column: x => x.FromLocationId,
                        principalTable: "WarehouseLocations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaterialBatchMovements_WarehouseLocations_ToLocationId",
                        column: x => x.ToLocationId,
                        principalTable: "WarehouseLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MaterialBatchMovements_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaterialBatchMovements_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaterialBatchMovements_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaterialBatchMovements_users_MovedById",
                        column: x => x.MovedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MaterialBatchMovements_BatchId",
                table: "MaterialBatchMovements",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialBatchMovements_CreatedById",
                table: "MaterialBatchMovements",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialBatchMovements_FromLocationId",
                table: "MaterialBatchMovements",
                column: "FromLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialBatchMovements_LastDeletedById",
                table: "MaterialBatchMovements",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialBatchMovements_LastUpdatedById",
                table: "MaterialBatchMovements",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialBatchMovements_MovedById",
                table: "MaterialBatchMovements",
                column: "MovedById");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialBatchMovements_ToLocationId",
                table: "MaterialBatchMovements",
                column: "ToLocationId");
        }
    }
}
