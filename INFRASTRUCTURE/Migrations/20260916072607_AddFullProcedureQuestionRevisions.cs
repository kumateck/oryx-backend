using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddFullProcedureQuestionRevisions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TemplateQuestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateAreaId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurposeId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    SubjectTypeId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplateQuestions_TemplateAreas_TemplateAreaId",
                        column: x => x.TemplateAreaId,
                        principalTable: "TemplateAreas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateQuestions_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TemplateQuestions_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TemplateQuestions_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TemplateQuestionRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateQuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Wording = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    AnswerType = table.Column<int>(type: "integer", nullable: false),
                    InputType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    UnitOfMeasureId = table.Column<Guid>(type: "uuid", nullable: true),
                    Required = table.Column<bool>(type: "boolean", nullable: false),
                    HelpText = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Minimum = table.Column<decimal>(type: "numeric(28,10)", precision: 28, scale: 10, nullable: true),
                    Maximum = table.Column<decimal>(type: "numeric(28,10)", precision: 28, scale: 10, nullable: true),
                    Sensitivity = table.Column<int>(type: "integer", nullable: false),
                    ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ReviewedById = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PublishedById = table.Column<Guid>(type: "uuid", nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RetiredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateQuestionRevisions", x => x.Id);
                    table.CheckConstraint("CK_TemplateQuestionRevision_AnswerType", "\"AnswerType\" BETWEEN 0 AND 12");
                    table.CheckConstraint("CK_TemplateQuestionRevision_ContentHash", "\"ContentHash\" ~ '^[a-f0-9]{64}$'");
                    table.CheckConstraint("CK_TemplateQuestionRevision_Range", "\"Minimum\" IS NULL OR \"Maximum\" IS NULL OR \"Minimum\" <= \"Maximum\"");
                    table.CheckConstraint("CK_TemplateQuestionRevision_Sensitivity", "\"Sensitivity\" BETWEEN 0 AND 2");
                    table.CheckConstraint("CK_TemplateQuestionRevision_Sequence", "\"Sequence\" > 0");
                    table.CheckConstraint("CK_TemplateQuestionRevision_Status", "\"Status\" BETWEEN 0 AND 3");
                    table.ForeignKey(
                        name: "FK_TemplateQuestionRevisions_TemplateQuestions_TemplateQuestio~",
                        column: x => x.TemplateQuestionId,
                        principalTable: "TemplateQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateQuestionRevisions_UnitOfMeasures_UnitOfMeasureId",
                        column: x => x.UnitOfMeasureId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateQuestionRevisions_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TemplateQuestionRevisions_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TemplateQuestionRevisions_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TemplateQuestionRevisions_users_PublishedById",
                        column: x => x.PublishedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateQuestionRevisions_users_ReviewedById",
                        column: x => x.ReviewedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TemplateQuestionCalculationReferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateQuestionRevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferencedQuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferencedQuestionRevisionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateQuestionCalculationReferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplateQuestionCalculationReferences_TemplateQuestionRevis~",
                        column: x => x.ReferencedQuestionRevisionId,
                        principalTable: "TemplateQuestionRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateQuestionCalculationReferences_TemplateQuestionRevi~1",
                        column: x => x.TemplateQuestionRevisionId,
                        principalTable: "TemplateQuestionRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateQuestionCalculationReferences_TemplateQuestions_Ref~",
                        column: x => x.ReferencedQuestionId,
                        principalTable: "TemplateQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TemplateQuestionOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateQuestionRevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Label = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Rank = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateQuestionOptions", x => x.Id);
                    table.CheckConstraint("CK_TemplateQuestionOption_Rank", "\"Rank\" >= 0");
                    table.ForeignKey(
                        name: "FK_TemplateQuestionOptions_TemplateQuestionRevisions_TemplateQ~",
                        column: x => x.TemplateQuestionRevisionId,
                        principalTable: "TemplateQuestionRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TemplateQuestionRevisionAudits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateQuestionRevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PriorStatus = table.Column<int>(type: "integer", nullable: true),
                    NewStatus = table.Column<int>(type: "integer", nullable: false),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Reason = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateQuestionRevisionAudits", x => x.Id);
                    table.CheckConstraint("CK_TemplateQuestionRevisionAudit_ContentHash", "\"ContentHash\" ~ '^[a-f0-9]{64}$'");
                    table.CheckConstraint("CK_TemplateQuestionRevisionAudit_Status", "\"NewStatus\" BETWEEN 0 AND 3 AND (\"PriorStatus\" IS NULL OR \"PriorStatus\" BETWEEN 0 AND 3)");
                    table.ForeignKey(
                        name: "FK_TemplateQuestionRevisionAudits_TemplateQuestionRevisions_Te~",
                        column: x => x.TemplateQuestionRevisionId,
                        principalTable: "TemplateQuestionRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateQuestionRevisionAudits_users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TemplateQuestionCalculationReferences_ReferencedQuestionId",
                table: "TemplateQuestionCalculationReferences",
                column: "ReferencedQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateQuestionCalculationReferences_ReferencedQuestionRev~",
                table: "TemplateQuestionCalculationReferences",
                column: "ReferencedQuestionRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateQuestionCalculationReferences_TemplateQuestionRevis~",
                table: "TemplateQuestionCalculationReferences",
                columns: new[] { "TemplateQuestionRevisionId", "ReferencedQuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemplateQuestionOptions_TemplateQuestionRevisionId_Rank",
                table: "TemplateQuestionOptions",
                columns: new[] { "TemplateQuestionRevisionId", "Rank" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemplateQuestionOptions_TemplateQuestionRevisionId_Value",
                table: "TemplateQuestionOptions",
                columns: new[] { "TemplateQuestionRevisionId", "Value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemplateQuestionRevisionAudits_ActorId",
                table: "TemplateQuestionRevisionAudits",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateQuestionRevisionAudits_CorrelationId",
                table: "TemplateQuestionRevisionAudits",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateQuestionRevisionAudits_TemplateQuestionRevisionId_O~",
                table: "TemplateQuestionRevisionAudits",
                columns: new[] { "TemplateQuestionRevisionId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TemplateQuestionRevisions_CreatedById",
                table: "TemplateQuestionRevisions",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateQuestionRevisions_LastDeletedById",
                table: "TemplateQuestionRevisions",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateQuestionRevisions_LastUpdatedById",
                table: "TemplateQuestionRevisions",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateQuestionRevisions_OneOpenRevision",
                table: "TemplateQuestionRevisions",
                column: "TemplateQuestionId",
                unique: true,
                filter: "\"Status\" IN (0, 1) AND \"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateQuestionRevisions_OnePublishedRevision",
                table: "TemplateQuestionRevisions",
                column: "TemplateQuestionId",
                unique: true,
                filter: "\"Status\" = 2 AND \"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateQuestionRevisions_PublishedById",
                table: "TemplateQuestionRevisions",
                column: "PublishedById");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateQuestionRevisions_ReviewedById",
                table: "TemplateQuestionRevisions",
                column: "ReviewedById");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateQuestionRevisions_TemplateQuestionId_Sequence",
                table: "TemplateQuestionRevisions",
                columns: new[] { "TemplateQuestionId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemplateQuestionRevisions_UnitOfMeasureId",
                table: "TemplateQuestionRevisions",
                column: "UnitOfMeasureId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateQuestions_CreatedById",
                table: "TemplateQuestions",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateQuestions_LastDeletedById",
                table: "TemplateQuestions",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateQuestions_LastUpdatedById",
                table: "TemplateQuestions",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateQuestions_TemplateAreaId_PurposeId_SubjectTypeId",
                table: "TemplateQuestions",
                columns: new[] { "TemplateAreaId", "PurposeId", "SubjectTypeId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TemplateQuestionCalculationReferences");

            migrationBuilder.DropTable(
                name: "TemplateQuestionOptions");

            migrationBuilder.DropTable(
                name: "TemplateQuestionRevisionAudits");

            migrationBuilder.DropTable(
                name: "TemplateQuestionRevisions");

            migrationBuilder.DropTable(
                name: "TemplateQuestions");
        }
    }
}
