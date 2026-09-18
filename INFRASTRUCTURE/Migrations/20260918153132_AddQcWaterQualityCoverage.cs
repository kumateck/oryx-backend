using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddQcWaterQualityCoverage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QcSamplingPoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Area = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    SamplingPointGroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QcSamplingPoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QcSamplingPoints_QcSamplingPointGroups_SamplingPointGroupId",
                        column: x => x.SamplingPointGroupId,
                        principalTable: "QcSamplingPointGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QcSamplingPoints_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QcSamplingPoints_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QcSamplingPoints_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "QcMonitoringPrograms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SamplingPointId = table.Column<Guid>(type: "uuid", nullable: false),
                    SpecificationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SpecificationVersion = table.Column<int>(type: "integer", nullable: false),
                    Frequency = table.Column<int>(type: "integer", nullable: false),
                    CustomIntervalDays = table.Column<int>(type: "integer", nullable: true),
                    LeadTimeDays = table.Column<int>(type: "integer", nullable: false),
                    NextDueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("PK_QcMonitoringPrograms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QcMonitoringPrograms_QcSamplingPoints_SamplingPointId",
                        column: x => x.SamplingPointId,
                        principalTable: "QcSamplingPoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QcMonitoringPrograms_QcSpecifications_SpecificationId",
                        column: x => x.SpecificationId,
                        principalTable: "QcSpecifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QcMonitoringPrograms_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QcMonitoringPrograms_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QcMonitoringPrograms_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "QcWaterQualityPeriods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SamplingPointId = table.Column<Guid>(type: "uuid", nullable: false),
                    TestRequestSubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ValidUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RetrospectiveReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ActivatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    ActivatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    HeldById = table.Column<Guid>(type: "uuid", nullable: true),
                    HeldAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    HoldReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QcWaterQualityPeriods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QcWaterQualityPeriods_QcSamplingPoints_SamplingPointId",
                        column: x => x.SamplingPointId,
                        principalTable: "QcSamplingPoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QcWaterQualityPeriods_QcTestRequestSubjects_TestRequestSubj~",
                        column: x => x.TestRequestSubjectId,
                        principalTable: "QcTestRequestSubjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QcWaterQualityPeriods_users_ActivatedById",
                        column: x => x.ActivatedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QcWaterQualityPeriods_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QcWaterQualityPeriods_users_HeldById",
                        column: x => x.HeldById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QcWaterQualityPeriods_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QcWaterQualityPeriods_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "QcWaterUseRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WaterQualityPeriodId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BatchManufacturingRecordId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductionActivityStepId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecordedById = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_QcWaterUseRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QcWaterUseRecords_BatchManufacturingRecords_BatchManufactur~",
                        column: x => x.BatchManufacturingRecordId,
                        principalTable: "BatchManufacturingRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QcWaterUseRecords_ProductionActivitySteps_ProductionActivit~",
                        column: x => x.ProductionActivityStepId,
                        principalTable: "ProductionActivitySteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QcWaterUseRecords_QcWaterQualityPeriods_WaterQualityPeriodId",
                        column: x => x.WaterQualityPeriodId,
                        principalTable: "QcWaterQualityPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QcWaterUseRecords_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QcWaterUseRecords_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QcWaterUseRecords_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QcWaterUseRecords_users_RecordedById",
                        column: x => x.RecordedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QcMonitoringPrograms_CreatedById",
                table: "QcMonitoringPrograms",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcMonitoringPrograms_LastDeletedById",
                table: "QcMonitoringPrograms",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcMonitoringPrograms_LastUpdatedById",
                table: "QcMonitoringPrograms",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcMonitoringPrograms_SamplingPointId",
                table: "QcMonitoringPrograms",
                column: "SamplingPointId");

            migrationBuilder.CreateIndex(
                name: "IX_QcMonitoringPrograms_SamplingPointId_SpecificationId",
                table: "QcMonitoringPrograms",
                columns: new[] { "SamplingPointId", "SpecificationId" });

            migrationBuilder.CreateIndex(
                name: "IX_QcMonitoringPrograms_SpecificationId_SpecificationVersion",
                table: "QcMonitoringPrograms",
                columns: new[] { "SpecificationId", "SpecificationVersion" });

            migrationBuilder.CreateIndex(
                name: "IX_QcMonitoringPrograms_Status_NextDueDate",
                table: "QcMonitoringPrograms",
                columns: new[] { "Status", "NextDueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_QcSamplingPoints_Code",
                table: "QcSamplingPoints",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_QcSamplingPoints_CreatedById",
                table: "QcSamplingPoints",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcSamplingPoints_LastDeletedById",
                table: "QcSamplingPoints",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcSamplingPoints_LastUpdatedById",
                table: "QcSamplingPoints",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcSamplingPoints_SamplingPointGroupId",
                table: "QcSamplingPoints",
                column: "SamplingPointGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_QcSamplingPoints_Type",
                table: "QcSamplingPoints",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_QcWaterQualityPeriods_ActivatedById",
                table: "QcWaterQualityPeriods",
                column: "ActivatedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcWaterQualityPeriods_CreatedById",
                table: "QcWaterQualityPeriods",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcWaterQualityPeriods_HeldById",
                table: "QcWaterQualityPeriods",
                column: "HeldById");

            migrationBuilder.CreateIndex(
                name: "IX_QcWaterQualityPeriods_LastDeletedById",
                table: "QcWaterQualityPeriods",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcWaterQualityPeriods_LastUpdatedById",
                table: "QcWaterQualityPeriods",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcWaterQualityPeriods_SamplingPointId_Status",
                table: "QcWaterQualityPeriods",
                columns: new[] { "SamplingPointId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_QcWaterQualityPeriods_Status_ValidUntil",
                table: "QcWaterQualityPeriods",
                columns: new[] { "Status", "ValidUntil" });

            migrationBuilder.CreateIndex(
                name: "IX_QcWaterQualityPeriods_TestRequestSubjectId",
                table: "QcWaterQualityPeriods",
                column: "TestRequestSubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_QcWaterUseRecords_BatchManufacturingRecordId",
                table: "QcWaterUseRecords",
                column: "BatchManufacturingRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_QcWaterUseRecords_CreatedById",
                table: "QcWaterUseRecords",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcWaterUseRecords_LastDeletedById",
                table: "QcWaterUseRecords",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcWaterUseRecords_LastUpdatedById",
                table: "QcWaterUseRecords",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcWaterUseRecords_ProductionActivityStepId",
                table: "QcWaterUseRecords",
                column: "ProductionActivityStepId");

            migrationBuilder.CreateIndex(
                name: "IX_QcWaterUseRecords_RecordedById",
                table: "QcWaterUseRecords",
                column: "RecordedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcWaterUseRecords_UsedAt",
                table: "QcWaterUseRecords",
                column: "UsedAt");

            migrationBuilder.CreateIndex(
                name: "IX_QcWaterUseRecords_WaterQualityPeriodId_Status",
                table: "QcWaterUseRecords",
                columns: new[] { "WaterQualityPeriodId", "Status" });

            migrationBuilder.AddForeignKey(
                name: "FK_QcTestRequestSubjects_QcSamplingPoints_SamplingPointId",
                table: "QcTestRequestSubjects",
                column: "SamplingPointId",
                principalTable: "QcSamplingPoints",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QcTestRequestSubjects_QcSamplingPoints_SamplingPointId",
                table: "QcTestRequestSubjects");

            migrationBuilder.DropTable(
                name: "QcMonitoringPrograms");

            migrationBuilder.DropTable(
                name: "QcWaterUseRecords");

            migrationBuilder.DropTable(
                name: "QcWaterQualityPeriods");

            migrationBuilder.DropTable(
                name: "QcSamplingPoints");
        }
    }
}
