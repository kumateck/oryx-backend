using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddRoutineQcAnalysis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RoutineTrackId",
                table: "Responses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RoutineTrackId",
                table: "FormAssignees",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RoutineArds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    AnalysisType = table.Column<int>(type: "integer", nullable: false),
                    SpecNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    FormId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    VerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VerifiedById = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoutineArds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoutineArds_Forms_FormId",
                        column: x => x.FormId,
                        principalTable: "Forms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoutineArds_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RoutineArds_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RoutineArds_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RoutineDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Cadence = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoutineDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoutineDefinitions_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RoutineDefinitions_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RoutineDefinitions_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RoutineCoaItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoutineArdId = table.Column<Guid>(type: "uuid", nullable: false),
                    FormFieldId = table.Column<Guid>(type: "uuid", nullable: false),
                    IncludeOnCoa = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayLabel = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    GroupName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    SpecificationText = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Unit = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Reference = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoutineCoaItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoutineCoaItems_FormFields_FormFieldId",
                        column: x => x.FormFieldId,
                        principalTable: "FormFields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoutineCoaItems_RoutineArds_RoutineArdId",
                        column: x => x.RoutineArdId,
                        principalTable: "RoutineArds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoutineCoaItems_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RoutineCoaItems_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RoutineCoaItems_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RoutineExecutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoutineCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Origin = table.Column<int>(type: "integer", nullable: false),
                    Cadence = table.Column<int>(type: "integer", nullable: true),
                    RoutineDefinitionId = table.Column<Guid>(type: "uuid", nullable: true),
                    PeriodStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RoutineDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EmergencyTrigger = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    EmergencyReason = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    RndTrialBatchId = table.Column<Guid>(type: "uuid", nullable: true),
                    DoneById = table.Column<Guid>(type: "uuid", nullable: true),
                    DoneAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_RoutineExecutions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoutineExecutions_RndTrialBatches_RndTrialBatchId",
                        column: x => x.RndTrialBatchId,
                        principalTable: "RndTrialBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoutineExecutions_RoutineDefinitions_RoutineDefinitionId",
                        column: x => x.RoutineDefinitionId,
                        principalTable: "RoutineDefinitions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RoutineExecutions_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RoutineExecutions_users_DoneById",
                        column: x => x.DoneById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RoutineExecutions_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RoutineExecutions_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RoutineAuditEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoutineExecutionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Detail = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoutineAuditEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoutineAuditEvents_RoutineExecutions_RoutineExecutionId",
                        column: x => x.RoutineExecutionId,
                        principalTable: "RoutineExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoutineAuditEvents_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RoutineAuditEvents_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RoutineAuditEvents_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RoutineSamples",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoutineExecutionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SamplingPoint = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    AreaName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CollectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoutineSamples", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoutineSamples_RoutineExecutions_RoutineExecutionId",
                        column: x => x.RoutineExecutionId,
                        principalTable: "RoutineExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoutineSamples_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RoutineSamples_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RoutineSamples_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RoutineCertificates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoutineSampleId = table.Column<Guid>(type: "uuid", nullable: false),
                    CertificateCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Combined = table.Column<bool>(type: "boolean", nullable: false),
                    RowsJson = table.Column<string>(type: "text", nullable: true),
                    IssuedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IssuedById = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoutineCertificates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoutineCertificates_RoutineSamples_RoutineSampleId",
                        column: x => x.RoutineSampleId,
                        principalTable: "RoutineSamples",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoutineCertificates_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RoutineCertificates_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RoutineCertificates_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RoutineTracks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoutineSampleId = table.Column<Guid>(type: "uuid", nullable: false),
                    AnalysisType = table.Column<int>(type: "integer", nullable: false),
                    RoutineArdId = table.Column<Guid>(type: "uuid", nullable: false),
                    FormId = table.Column<Guid>(type: "uuid", nullable: false),
                    FormRevisionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CoaItemsSnapshotJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoutineTracks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoutineTracks_RoutineArds_RoutineArdId",
                        column: x => x.RoutineArdId,
                        principalTable: "RoutineArds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoutineTracks_RoutineSamples_RoutineSampleId",
                        column: x => x.RoutineSampleId,
                        principalTable: "RoutineSamples",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoutineTracks_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RoutineTracks_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RoutineTracks_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Responses_RoutineTrackId",
                table: "Responses",
                column: "RoutineTrackId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FormAssignees_RoutineTrackId",
                table: "FormAssignees",
                column: "RoutineTrackId");

            migrationBuilder.CreateIndex(
                name: "IX_RoutineArds_CreatedById",
                table: "RoutineArds",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RoutineArds_FormId",
                table: "RoutineArds",
                column: "FormId");

            migrationBuilder.CreateIndex(
                name: "IX_RoutineArds_LastDeletedById",
                table: "RoutineArds",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_RoutineArds_LastUpdatedById",
                table: "RoutineArds",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RoutineArds_Type_AnalysisType",
                table: "RoutineArds",
                columns: new[] { "Type", "AnalysisType" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RoutineAuditEvents_CreatedById",
                table: "RoutineAuditEvents",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RoutineAuditEvents_LastDeletedById",
                table: "RoutineAuditEvents",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_RoutineAuditEvents_LastUpdatedById",
                table: "RoutineAuditEvents",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RoutineAuditEvents_RoutineExecutionId",
                table: "RoutineAuditEvents",
                column: "RoutineExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_RoutineCertificates_CreatedById",
                table: "RoutineCertificates",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RoutineCertificates_LastDeletedById",
                table: "RoutineCertificates",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_RoutineCertificates_LastUpdatedById",
                table: "RoutineCertificates",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RoutineCertificates_RoutineSampleId",
                table: "RoutineCertificates",
                column: "RoutineSampleId",
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RoutineCoaItems_CreatedById",
                table: "RoutineCoaItems",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RoutineCoaItems_FormFieldId",
                table: "RoutineCoaItems",
                column: "FormFieldId");

            migrationBuilder.CreateIndex(
                name: "IX_RoutineCoaItems_LastDeletedById",
                table: "RoutineCoaItems",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_RoutineCoaItems_LastUpdatedById",
                table: "RoutineCoaItems",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RoutineCoaItems_RoutineArdId_FormFieldId",
                table: "RoutineCoaItems",
                columns: new[] { "RoutineArdId", "FormFieldId" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RoutineDefinitions_CreatedById",
                table: "RoutineDefinitions",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RoutineDefinitions_LastDeletedById",
                table: "RoutineDefinitions",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_RoutineDefinitions_LastUpdatedById",
                table: "RoutineDefinitions",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RoutineExecutions_CreatedById",
                table: "RoutineExecutions",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RoutineExecutions_DoneById",
                table: "RoutineExecutions",
                column: "DoneById");

            migrationBuilder.CreateIndex(
                name: "IX_RoutineExecutions_LastDeletedById",
                table: "RoutineExecutions",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_RoutineExecutions_LastUpdatedById",
                table: "RoutineExecutions",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RoutineExecutions_RndTrialBatchId",
                table: "RoutineExecutions",
                column: "RndTrialBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_RoutineExecutions_RoutineCode",
                table: "RoutineExecutions",
                column: "RoutineCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoutineExecutions_RoutineDefinitionId",
                table: "RoutineExecutions",
                column: "RoutineDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_RoutineSamples_CreatedById",
                table: "RoutineSamples",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RoutineSamples_LastDeletedById",
                table: "RoutineSamples",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_RoutineSamples_LastUpdatedById",
                table: "RoutineSamples",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RoutineSamples_RoutineExecutionId",
                table: "RoutineSamples",
                column: "RoutineExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_RoutineTracks_CreatedById",
                table: "RoutineTracks",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RoutineTracks_LastDeletedById",
                table: "RoutineTracks",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_RoutineTracks_LastUpdatedById",
                table: "RoutineTracks",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RoutineTracks_RoutineArdId",
                table: "RoutineTracks",
                column: "RoutineArdId");

            migrationBuilder.CreateIndex(
                name: "IX_RoutineTracks_RoutineSampleId_AnalysisType",
                table: "RoutineTracks",
                columns: new[] { "RoutineSampleId", "AnalysisType" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_FormAssignees_RoutineTracks_RoutineTrackId",
                table: "FormAssignees",
                column: "RoutineTrackId",
                principalTable: "RoutineTracks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Responses_RoutineTracks_RoutineTrackId",
                table: "Responses",
                column: "RoutineTrackId",
                principalTable: "RoutineTracks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FormAssignees_RoutineTracks_RoutineTrackId",
                table: "FormAssignees");

            migrationBuilder.DropForeignKey(
                name: "FK_Responses_RoutineTracks_RoutineTrackId",
                table: "Responses");

            migrationBuilder.DropTable(
                name: "RoutineAuditEvents");

            migrationBuilder.DropTable(
                name: "RoutineCertificates");

            migrationBuilder.DropTable(
                name: "RoutineCoaItems");

            migrationBuilder.DropTable(
                name: "RoutineTracks");

            migrationBuilder.DropTable(
                name: "RoutineArds");

            migrationBuilder.DropTable(
                name: "RoutineSamples");

            migrationBuilder.DropTable(
                name: "RoutineExecutions");

            migrationBuilder.DropTable(
                name: "RoutineDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_Responses_RoutineTrackId",
                table: "Responses");

            migrationBuilder.DropIndex(
                name: "IX_FormAssignees_RoutineTrackId",
                table: "FormAssignees");

            migrationBuilder.DropColumn(
                name: "RoutineTrackId",
                table: "Responses");

            migrationBuilder.DropColumn(
                name: "RoutineTrackId",
                table: "FormAssignees");
        }
    }
}
