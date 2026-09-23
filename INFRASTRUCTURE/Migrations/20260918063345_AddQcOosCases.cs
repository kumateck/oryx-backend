using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddQcOosCases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QcOosCases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Approved = table.Column<bool>(type: "boolean", nullable: false),
                    WorksheetInstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    OpenedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ObservedValue = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    BreachedLimit = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SpecificationCharacteristicId = table.Column<Guid>(type: "uuid", nullable: true),
                    InvestigationDetails = table.Column<string>(type: "text", nullable: true),
                    RootCauseAnalysis = table.Column<string>(type: "text", nullable: true),
                    CorrectiveActions = table.Column<string>(type: "text", nullable: true),
                    PreventiveActions = table.Column<string>(type: "text", nullable: true),
                    InvestigatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    InvestigatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RetestAuthorizedById = table.Column<Guid>(type: "uuid", nullable: true),
                    RetestAuthorizedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RetestWorksheetInstanceId = table.Column<Guid>(type: "uuid", nullable: true),
                    DispositionOutcome = table.Column<int>(type: "integer", nullable: true),
                    DispositionById = table.Column<Guid>(type: "uuid", nullable: true),
                    DispositionAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DispositionComments = table.Column<string>(type: "text", nullable: true),
                    QuarantinedMaterialBatchId = table.Column<Guid>(type: "uuid", nullable: true),
                    QuarantinedBatchManufacturingRecordId = table.Column<Guid>(type: "uuid", nullable: true),
                    QuarantinedFromBatchStatus = table.Column<int>(type: "integer", nullable: true),
                    QuarantinedFromBatchManufacturingStatus = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QcOosCases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QcOosCases_BatchManufacturingRecords_QuarantinedBatchManufa~",
                        column: x => x.QuarantinedBatchManufacturingRecordId,
                        principalTable: "BatchManufacturingRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QcOosCases_MaterialBatches_QuarantinedMaterialBatchId",
                        column: x => x.QuarantinedMaterialBatchId,
                        principalTable: "MaterialBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QcOosCases_QcSpecificationCharacteristics_SpecificationChar~",
                        column: x => x.SpecificationCharacteristicId,
                        principalTable: "QcSpecificationCharacteristics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QcOosCases_QcWorksheetInstances_RetestWorksheetInstanceId",
                        column: x => x.RetestWorksheetInstanceId,
                        principalTable: "QcWorksheetInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QcOosCases_QcWorksheetInstances_WorksheetInstanceId",
                        column: x => x.WorksheetInstanceId,
                        principalTable: "QcWorksheetInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QcOosCases_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QcOosCases_users_DispositionById",
                        column: x => x.DispositionById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QcOosCases_users_InvestigatedById",
                        column: x => x.InvestigatedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QcOosCases_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QcOosCases_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QcOosCases_users_RetestAuthorizedById",
                        column: x => x.RetestAuthorizedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QcOosCases_CreatedById",
                table: "QcOosCases",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcOosCases_DispositionById",
                table: "QcOosCases",
                column: "DispositionById");

            migrationBuilder.CreateIndex(
                name: "IX_QcOosCases_InvestigatedById",
                table: "QcOosCases",
                column: "InvestigatedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcOosCases_LastDeletedById",
                table: "QcOosCases",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcOosCases_LastUpdatedById",
                table: "QcOosCases",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcOosCases_OpenedAt",
                table: "QcOosCases",
                column: "OpenedAt");

            migrationBuilder.CreateIndex(
                name: "IX_QcOosCases_QuarantinedBatchManufacturingRecordId",
                table: "QcOosCases",
                column: "QuarantinedBatchManufacturingRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_QcOosCases_QuarantinedMaterialBatchId",
                table: "QcOosCases",
                column: "QuarantinedMaterialBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_QcOosCases_RetestAuthorizedById",
                table: "QcOosCases",
                column: "RetestAuthorizedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcOosCases_RetestWorksheetInstanceId",
                table: "QcOosCases",
                column: "RetestWorksheetInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_QcOosCases_SpecificationCharacteristicId",
                table: "QcOosCases",
                column: "SpecificationCharacteristicId");

            migrationBuilder.CreateIndex(
                name: "IX_QcOosCases_Status",
                table: "QcOosCases",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_QcOosCases_Status_OpenedAt",
                table: "QcOosCases",
                columns: new[] { "Status", "OpenedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_QcOosCases_WorksheetInstanceId_FieldKey",
                table: "QcOosCases",
                columns: new[] { "WorksheetInstanceId", "FieldKey" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QcOosCases");
        }
    }
}
