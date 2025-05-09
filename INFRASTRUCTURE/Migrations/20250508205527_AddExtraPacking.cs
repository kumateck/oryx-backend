using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddExtraPacking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductionExtraPackings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionScheduleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    UoMId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    IssuedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IssuedById = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionExtraPackings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionExtraPackings_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionExtraPackings_ProductionSchedules_ProductionSched~",
                        column: x => x.ProductionScheduleId,
                        principalTable: "ProductionSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionExtraPackings_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionExtraPackings_UnitOfMeasures_UoMId",
                        column: x => x.UoMId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionExtraPackings_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionExtraPackings_users_IssuedById",
                        column: x => x.IssuedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionExtraPackings_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionExtraPackings_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionExtraPackings_CreatedById",
                table: "ProductionExtraPackings",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionExtraPackings_IssuedById",
                table: "ProductionExtraPackings",
                column: "IssuedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionExtraPackings_LastDeletedById",
                table: "ProductionExtraPackings",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionExtraPackings_LastUpdatedById",
                table: "ProductionExtraPackings",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionExtraPackings_MaterialId",
                table: "ProductionExtraPackings",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionExtraPackings_ProductId",
                table: "ProductionExtraPackings",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionExtraPackings_ProductionScheduleId",
                table: "ProductionExtraPackings",
                column: "ProductionScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionExtraPackings_UoMId",
                table: "ProductionExtraPackings",
                column: "UoMId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductionExtraPackings");
        }
    }
}
