using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddQcTestRequestsAndInstances : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QcTestRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    SpecificationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SpecificationVersion = table.Column<int>(type: "integer", nullable: false),
                    ScheduleOrigin = table.Column<int>(type: "integer", nullable: false),
                    UnscheduledReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ArNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IssueNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IssuedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IssuedById = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_QcTestRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QcTestRequests_QcSpecifications_SpecificationId",
                        column: x => x.SpecificationId,
                        principalTable: "QcSpecifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QcTestRequests_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QcTestRequests_users_IssuedById",
                        column: x => x.IssuedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QcTestRequests_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QcTestRequests_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "QcTestRequestSubjects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TestRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectRef = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SubjectLabel = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ArNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SamplingPointGroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    SamplingPointId = table.Column<Guid>(type: "uuid", nullable: true),
                    MaterialBatchId = table.Column<Guid>(type: "uuid", nullable: true),
                    BatchManufacturingRecordId = table.Column<Guid>(type: "uuid", nullable: true),
                    CollectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QcTestRequestSubjects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QcTestRequestSubjects_BatchManufacturingRecords_BatchManufa~",
                        column: x => x.BatchManufacturingRecordId,
                        principalTable: "BatchManufacturingRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QcTestRequestSubjects_MaterialBatches_MaterialBatchId",
                        column: x => x.MaterialBatchId,
                        principalTable: "MaterialBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QcTestRequestSubjects_QcSamplingPointGroups_SamplingPointGr~",
                        column: x => x.SamplingPointGroupId,
                        principalTable: "QcSamplingPointGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QcTestRequestSubjects_QcTestRequests_TestRequestId",
                        column: x => x.TestRequestId,
                        principalTable: "QcTestRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QcTestRequestSubjects_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QcTestRequestSubjects_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QcTestRequestSubjects_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "QcWorksheetInstances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Approved = table.Column<bool>(type: "boolean", nullable: false),
                    TestRequestSubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorksheetTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorksheetTemplateVersion = table.Column<int>(type: "integer", nullable: false),
                    AnalysisType = table.Column<int>(type: "integer", nullable: false),
                    AssignedToId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssignedById = table.Column<Guid>(type: "uuid", nullable: true),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RetestOfInstanceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReturnedForCorrectionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ReturnedForCorrectionAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReturnedForCorrectionById = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QcWorksheetInstances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QcWorksheetInstances_QcTestRequestSubjects_TestRequestSubje~",
                        column: x => x.TestRequestSubjectId,
                        principalTable: "QcTestRequestSubjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QcWorksheetInstances_QcWorksheetInstances_RetestOfInstanceId",
                        column: x => x.RetestOfInstanceId,
                        principalTable: "QcWorksheetInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QcWorksheetInstances_QcWorksheetTemplates_WorksheetTemplate~",
                        column: x => x.WorksheetTemplateId,
                        principalTable: "QcWorksheetTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QcWorksheetInstances_users_AssignedById",
                        column: x => x.AssignedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QcWorksheetInstances_users_AssignedToId",
                        column: x => x.AssignedToId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QcWorksheetInstances_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QcWorksheetInstances_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QcWorksheetInstances_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QcWorksheetInstances_users_ReturnedForCorrectionById",
                        column: x => x.ReturnedForCorrectionById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QcWorksheetFieldValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorksheetInstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RowIndex = table.Column<int>(type: "integer", nullable: true),
                    ColumnKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Value = table.Column<string>(type: "text", nullable: true),
                    EnteredById = table.Column<Guid>(type: "uuid", nullable: false),
                    EnteredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedFromInstanceId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QcWorksheetFieldValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QcWorksheetFieldValues_QcWorksheetInstances_ResolvedFromIns~",
                        column: x => x.ResolvedFromInstanceId,
                        principalTable: "QcWorksheetInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QcWorksheetFieldValues_QcWorksheetInstances_WorksheetInstan~",
                        column: x => x.WorksheetInstanceId,
                        principalTable: "QcWorksheetInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QcWorksheetFieldValues_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QcWorksheetFieldValues_users_EnteredById",
                        column: x => x.EnteredById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QcWorksheetFieldValues_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QcWorksheetFieldValues_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "QcWorksheetInstanceReassignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorksheetInstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ToUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReassignedById = table.Column<Guid>(type: "uuid", nullable: false),
                    ReassignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QcWorksheetInstanceReassignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QcWorksheetInstanceReassignments_QcWorksheetInstances_Works~",
                        column: x => x.WorksheetInstanceId,
                        principalTable: "QcWorksheetInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QcWorksheetInstanceReassignments_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QcWorksheetInstanceReassignments_users_FromUserId",
                        column: x => x.FromUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QcWorksheetInstanceReassignments_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QcWorksheetInstanceReassignments_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QcWorksheetInstanceReassignments_users_ReassignedById",
                        column: x => x.ReassignedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QcWorksheetInstanceReassignments_users_ToUserId",
                        column: x => x.ToUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QcTestRequests_ArNumber",
                table: "QcTestRequests",
                column: "ArNumber");

            migrationBuilder.CreateIndex(
                name: "IX_QcTestRequests_CreatedById",
                table: "QcTestRequests",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcTestRequests_IssuedById",
                table: "QcTestRequests",
                column: "IssuedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcTestRequests_LastDeletedById",
                table: "QcTestRequests",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcTestRequests_LastUpdatedById",
                table: "QcTestRequests",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcTestRequests_SpecificationId_SpecificationVersion",
                table: "QcTestRequests",
                columns: new[] { "SpecificationId", "SpecificationVersion" });

            migrationBuilder.CreateIndex(
                name: "IX_QcTestRequests_Status",
                table: "QcTestRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_QcTestRequests_Type_Status",
                table: "QcTestRequests",
                columns: new[] { "Type", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_QcTestRequestSubjects_BatchManufacturingRecordId",
                table: "QcTestRequestSubjects",
                column: "BatchManufacturingRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_QcTestRequestSubjects_CreatedById",
                table: "QcTestRequestSubjects",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcTestRequestSubjects_LastDeletedById",
                table: "QcTestRequestSubjects",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcTestRequestSubjects_LastUpdatedById",
                table: "QcTestRequestSubjects",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcTestRequestSubjects_MaterialBatchId",
                table: "QcTestRequestSubjects",
                column: "MaterialBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_QcTestRequestSubjects_SamplingPointGroupId",
                table: "QcTestRequestSubjects",
                column: "SamplingPointGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_QcTestRequestSubjects_SamplingPointId",
                table: "QcTestRequestSubjects",
                column: "SamplingPointId");

            migrationBuilder.CreateIndex(
                name: "IX_QcTestRequestSubjects_SubjectRef",
                table: "QcTestRequestSubjects",
                column: "SubjectRef");

            migrationBuilder.CreateIndex(
                name: "IX_QcTestRequestSubjects_TestRequestId",
                table: "QcTestRequestSubjects",
                column: "TestRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_QcTestRequestSubjects_TestRequestId_SubjectRef",
                table: "QcTestRequestSubjects",
                columns: new[] { "TestRequestId", "SubjectRef" });

            migrationBuilder.CreateIndex(
                name: "IX_QcWorksheetFieldValues_ColumnKey_Value",
                table: "QcWorksheetFieldValues",
                columns: new[] { "ColumnKey", "Value" });

            migrationBuilder.CreateIndex(
                name: "IX_QcWorksheetFieldValues_CreatedById",
                table: "QcWorksheetFieldValues",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcWorksheetFieldValues_EnteredById",
                table: "QcWorksheetFieldValues",
                column: "EnteredById");

            migrationBuilder.CreateIndex(
                name: "IX_QcWorksheetFieldValues_LastDeletedById",
                table: "QcWorksheetFieldValues",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcWorksheetFieldValues_LastUpdatedById",
                table: "QcWorksheetFieldValues",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcWorksheetFieldValues_ResolvedFromInstanceId",
                table: "QcWorksheetFieldValues",
                column: "ResolvedFromInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_QcWorksheetFieldValues_WorksheetInstanceId_FieldKey",
                table: "QcWorksheetFieldValues",
                columns: new[] { "WorksheetInstanceId", "FieldKey" });

            migrationBuilder.CreateIndex(
                name: "IX_QcWorksheetInstanceReassignments_CreatedById",
                table: "QcWorksheetInstanceReassignments",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcWorksheetInstanceReassignments_FromUserId",
                table: "QcWorksheetInstanceReassignments",
                column: "FromUserId");

            migrationBuilder.CreateIndex(
                name: "IX_QcWorksheetInstanceReassignments_LastDeletedById",
                table: "QcWorksheetInstanceReassignments",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcWorksheetInstanceReassignments_LastUpdatedById",
                table: "QcWorksheetInstanceReassignments",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcWorksheetInstanceReassignments_ReassignedById",
                table: "QcWorksheetInstanceReassignments",
                column: "ReassignedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcWorksheetInstanceReassignments_ToUserId",
                table: "QcWorksheetInstanceReassignments",
                column: "ToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_QcWorksheetInstanceReassignments_WorksheetInstanceId_Reassi~",
                table: "QcWorksheetInstanceReassignments",
                columns: new[] { "WorksheetInstanceId", "ReassignedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_QcWorksheetInstances_AnalysisType_Status",
                table: "QcWorksheetInstances",
                columns: new[] { "AnalysisType", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_QcWorksheetInstances_AssignedById",
                table: "QcWorksheetInstances",
                column: "AssignedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcWorksheetInstances_AssignedToId_Status",
                table: "QcWorksheetInstances",
                columns: new[] { "AssignedToId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_QcWorksheetInstances_CreatedById",
                table: "QcWorksheetInstances",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcWorksheetInstances_LastDeletedById",
                table: "QcWorksheetInstances",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcWorksheetInstances_LastUpdatedById",
                table: "QcWorksheetInstances",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcWorksheetInstances_RetestOfInstanceId",
                table: "QcWorksheetInstances",
                column: "RetestOfInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_QcWorksheetInstances_ReturnedForCorrectionById",
                table: "QcWorksheetInstances",
                column: "ReturnedForCorrectionById");

            migrationBuilder.CreateIndex(
                name: "IX_QcWorksheetInstances_Status",
                table: "QcWorksheetInstances",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_QcWorksheetInstances_TestRequestSubjectId",
                table: "QcWorksheetInstances",
                column: "TestRequestSubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_QcWorksheetInstances_WorksheetTemplateId_Status",
                table: "QcWorksheetInstances",
                columns: new[] { "WorksheetTemplateId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_QcWorksheetInstances_WorksheetTemplateId_WorksheetTemplateV~",
                table: "QcWorksheetInstances",
                columns: new[] { "WorksheetTemplateId", "WorksheetTemplateVersion" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QcWorksheetFieldValues");

            migrationBuilder.DropTable(
                name: "QcWorksheetInstanceReassignments");

            migrationBuilder.DropTable(
                name: "QcWorksheetInstances");

            migrationBuilder.DropTable(
                name: "QcTestRequestSubjects");

            migrationBuilder.DropTable(
                name: "QcTestRequests");
        }
    }
}
