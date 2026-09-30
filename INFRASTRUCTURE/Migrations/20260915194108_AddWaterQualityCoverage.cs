using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddWaterQualityCoverage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WaterQualityPeriods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoutineCertificateId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoutineSampleId = table.Column<Guid>(type: "uuid", nullable: false),
                    SamplingPoint = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ValidUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RetrospectiveReason = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    ApprovedById = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    HoldReason = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    HeldById = table.Column<Guid>(type: "uuid", nullable: true),
                    HeldAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WaterQualityPeriods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WaterQualityPeriods_RoutineCertificates_RoutineCertificateId",
                        column: x => x.RoutineCertificateId,
                        principalTable: "RoutineCertificates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WaterQualityPeriods_RoutineSamples_RoutineSampleId",
                        column: x => x.RoutineSampleId,
                        principalTable: "RoutineSamples",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WaterQualityPeriods_users_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WaterQualityPeriods_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WaterQualityPeriods_users_HeldById",
                        column: x => x.HeldById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WaterQualityPeriods_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WaterQualityPeriods_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WaterUseRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WaterQualityPeriodId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SamplingPoint = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    BatchManufacturingRecordId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductionActivityStepId = table.Column<Guid>(type: "uuid", nullable: true),
                    RndTrialBatchId = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_WaterUseRecords", x => x.Id);
                    table.CheckConstraint("CK_WaterUseRecords_Subject", "(\"BatchManufacturingRecordId\" IS NOT NULL AND \"RndTrialBatchId\" IS NULL) OR (\"BatchManufacturingRecordId\" IS NULL AND \"ProductionActivityStepId\" IS NULL AND \"RndTrialBatchId\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_WaterUseRecords_BatchManufacturingRecords_BatchManufacturin~",
                        column: x => x.BatchManufacturingRecordId,
                        principalTable: "BatchManufacturingRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WaterUseRecords_ProductionActivitySteps_ProductionActivityS~",
                        column: x => x.ProductionActivityStepId,
                        principalTable: "ProductionActivitySteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WaterUseRecords_RndTrialBatches_RndTrialBatchId",
                        column: x => x.RndTrialBatchId,
                        principalTable: "RndTrialBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WaterUseRecords_WaterQualityPeriods_WaterQualityPeriodId",
                        column: x => x.WaterQualityPeriodId,
                        principalTable: "WaterQualityPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WaterUseRecords_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WaterUseRecords_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WaterUseRecords_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_WaterQualityPeriods_ApprovedById",
                table: "WaterQualityPeriods",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_WaterQualityPeriods_CreatedById",
                table: "WaterQualityPeriods",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_WaterQualityPeriods_HeldById",
                table: "WaterQualityPeriods",
                column: "HeldById");

            migrationBuilder.CreateIndex(
                name: "IX_WaterQualityPeriods_LastDeletedById",
                table: "WaterQualityPeriods",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_WaterQualityPeriods_LastUpdatedById",
                table: "WaterQualityPeriods",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_WaterQualityPeriods_RoutineCertificateId",
                table: "WaterQualityPeriods",
                column: "RoutineCertificateId",
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_WaterQualityPeriods_RoutineSampleId",
                table: "WaterQualityPeriods",
                column: "RoutineSampleId");

            migrationBuilder.CreateIndex(
                name: "IX_WaterUseRecords_BatchManufacturingRecordId",
                table: "WaterUseRecords",
                column: "BatchManufacturingRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_WaterUseRecords_CreatedById",
                table: "WaterUseRecords",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_WaterUseRecords_LastDeletedById",
                table: "WaterUseRecords",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_WaterUseRecords_LastUpdatedById",
                table: "WaterUseRecords",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_WaterUseRecords_ProductionActivityStepId",
                table: "WaterUseRecords",
                column: "ProductionActivityStepId");

            migrationBuilder.CreateIndex(
                name: "IX_WaterUseRecords_RndTrialBatchId",
                table: "WaterUseRecords",
                column: "RndTrialBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_WaterUseRecords_WaterQualityPeriodId",
                table: "WaterUseRecords",
                column: "WaterQualityPeriodId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WaterUseRecords");

            migrationBuilder.DropTable(
                name: "WaterQualityPeriods");
        }
    }
}
