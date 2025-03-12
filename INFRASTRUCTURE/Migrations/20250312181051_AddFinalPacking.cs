using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddFinalPacking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FinalPackings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionScheduleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionActivityStepId = table.Column<Guid>(type: "uuid", nullable: true),
                    NumberOfBottlesPerShipper = table.Column<decimal>(type: "numeric", nullable: false),
                    NUmberOfFullShipperPacked = table.Column<decimal>(type: "numeric", nullable: false),
                    LeftOver = table.Column<decimal>(type: "numeric", nullable: false),
                    BatchSize = table.Column<decimal>(type: "numeric", nullable: false),
                    AverageVolumeFilledPerBottle = table.Column<decimal>(type: "numeric", nullable: false),
                    PackSize = table.Column<decimal>(type: "numeric", nullable: false),
                    ExpectedYield = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalQuantityPacked = table.Column<decimal>(type: "numeric", nullable: false),
                    QualityControlAnalyticalSample = table.Column<decimal>(type: "numeric", nullable: false),
                    RetainedSamples = table.Column<decimal>(type: "numeric", nullable: false),
                    StabilitySamples = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalNumberOfBottles = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalGainOrLoss = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinalPackings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinalPackings_ProductionActivitySteps_ProductionActivitySte~",
                        column: x => x.ProductionActivityStepId,
                        principalTable: "ProductionActivitySteps",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FinalPackings_ProductionSchedules_ProductionScheduleId",
                        column: x => x.ProductionScheduleId,
                        principalTable: "ProductionSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FinalPackings_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FinalPackings_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FinalPackings_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FinalPackings_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FinalPackingMaterials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FinalPackingId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceivedQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    SubsequentDeliveredQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalReceivedQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    PackedQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    ReturnedQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    RejectedQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    SampledQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalAccountedForQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    PercentageLoss = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinalPackingMaterials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinalPackingMaterials_FinalPackings_FinalPackingId",
                        column: x => x.FinalPackingId,
                        principalTable: "FinalPackings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FinalPackingMaterials_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FinalPackingMaterials_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FinalPackingMaterials_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FinalPackingMaterials_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_FinalPackingMaterials_CreatedById",
                table: "FinalPackingMaterials",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_FinalPackingMaterials_FinalPackingId",
                table: "FinalPackingMaterials",
                column: "FinalPackingId");

            migrationBuilder.CreateIndex(
                name: "IX_FinalPackingMaterials_LastDeletedById",
                table: "FinalPackingMaterials",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_FinalPackingMaterials_LastUpdatedById",
                table: "FinalPackingMaterials",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_FinalPackingMaterials_MaterialId",
                table: "FinalPackingMaterials",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_FinalPackings_CreatedById",
                table: "FinalPackings",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_FinalPackings_LastDeletedById",
                table: "FinalPackings",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_FinalPackings_LastUpdatedById",
                table: "FinalPackings",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_FinalPackings_ProductId",
                table: "FinalPackings",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_FinalPackings_ProductionActivityStepId",
                table: "FinalPackings",
                column: "ProductionActivityStepId");

            migrationBuilder.CreateIndex(
                name: "IX_FinalPackings_ProductionScheduleId",
                table: "FinalPackings",
                column: "ProductionScheduleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FinalPackingMaterials");

            migrationBuilder.DropTable(
                name: "FinalPackings");
        }
    }
}
