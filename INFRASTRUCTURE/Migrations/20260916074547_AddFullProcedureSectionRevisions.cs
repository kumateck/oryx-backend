using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddFullProcedureSectionRevisions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TemplateQuestionCalculationReferences_TemplateQuestionRevis~",
                table: "TemplateQuestionCalculationReferences");

            migrationBuilder.DropForeignKey(
                name: "FK_TemplateQuestionCalculationReferences_TemplateQuestionRevi~1",
                table: "TemplateQuestionCalculationReferences");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_TemplateQuestionRevisions_Id_TemplateQuestionId",
                table: "TemplateQuestionRevisions",
                columns: new[] { "Id", "TemplateQuestionId" });

            migrationBuilder.CreateTable(
                name: "TemplateSections",
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
                    table.PrimaryKey("PK_TemplateSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplateSections_TemplateAreas_TemplateAreaId",
                        column: x => x.TemplateAreaId,
                        principalTable: "TemplateAreas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateSections_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TemplateSections_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TemplateSections_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TemplateSectionRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateSectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
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
                    table.PrimaryKey("PK_TemplateSectionRevisions", x => x.Id);
                    table.CheckConstraint("CK_TemplateSectionRevision_ContentHash", "\"ContentHash\" ~ '^[a-f0-9]{64}$'");
                    table.CheckConstraint("CK_TemplateSectionRevision_Sequence", "\"Sequence\" > 0");
                    table.CheckConstraint("CK_TemplateSectionRevision_Status", "\"Status\" BETWEEN 0 AND 3");
                    table.ForeignKey(
                        name: "FK_TemplateSectionRevisions_TemplateSections_TemplateSectionId",
                        column: x => x.TemplateSectionId,
                        principalTable: "TemplateSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateSectionRevisions_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TemplateSectionRevisions_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TemplateSectionRevisions_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TemplateSectionRevisions_users_PublishedById",
                        column: x => x.PublishedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateSectionRevisions_users_ReviewedById",
                        column: x => x.ReviewedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TemplateSectionQuestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateSectionRevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateQuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateQuestionRevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateSectionQuestions", x => x.Id);
                    table.UniqueConstraint("AK_TemplateSectionQuestions_TemplateSectionRevisionId_Id", x => new { x.TemplateSectionRevisionId, x.Id });
                    table.CheckConstraint("CK_TemplateSectionQuestion_Order", "\"Order\" >= 0");
                    table.ForeignKey(
                        name: "FK_TemplateSectionQuestions_TemplateQuestionRevisions_Template~",
                        columns: x => new { x.TemplateQuestionRevisionId, x.TemplateQuestionId },
                        principalTable: "TemplateQuestionRevisions",
                        principalColumns: new[] { "Id", "TemplateQuestionId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateSectionQuestions_TemplateQuestions_TemplateQuestion~",
                        column: x => x.TemplateQuestionId,
                        principalTable: "TemplateQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateSectionQuestions_TemplateSectionRevisions_TemplateS~",
                        column: x => x.TemplateSectionRevisionId,
                        principalTable: "TemplateSectionRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TemplateSectionRevisionAudits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateSectionRevisionId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_TemplateSectionRevisionAudits", x => x.Id);
                    table.CheckConstraint("CK_TemplateSectionRevisionAudit_ContentHash", "\"ContentHash\" ~ '^[a-f0-9]{64}$'");
                    table.CheckConstraint("CK_TemplateSectionRevisionAudit_Status", "\"NewStatus\" BETWEEN 0 AND 3 AND (\"PriorStatus\" IS NULL OR \"PriorStatus\" BETWEEN 0 AND 3)");
                    table.ForeignKey(
                        name: "FK_TemplateSectionRevisionAudits_TemplateSectionRevisions_Temp~",
                        column: x => x.TemplateSectionRevisionId,
                        principalTable: "TemplateSectionRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateSectionRevisionAudits_users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TemplateSectionConditionalRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateSectionRevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetSectionQuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    DependsOnSectionQuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Operator = table.Column<int>(type: "integer", nullable: false),
                    ComparisonValue = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateSectionConditionalRules", x => x.Id);
                    table.CheckConstraint("CK_TemplateSectionConditionalRule_Operator", "\"Operator\" BETWEEN 0 AND 2");
                    table.CheckConstraint("CK_TemplateSectionConditionalRule_Value", "(\"Operator\" = 2 AND \"ComparisonValue\" IS NULL) OR (\"Operator\" IN (0, 1) AND length(trim(\"ComparisonValue\")) > 0)");
                    table.ForeignKey(
                        name: "FK_TemplateSectionConditionalRules_TemplateSectionQuestions_Te~",
                        columns: x => new { x.TemplateSectionRevisionId, x.DependsOnSectionQuestionId },
                        principalTable: "TemplateSectionQuestions",
                        principalColumns: new[] { "TemplateSectionRevisionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateSectionConditionalRules_TemplateSectionQuestions_T~1",
                        columns: x => new { x.TemplateSectionRevisionId, x.TargetSectionQuestionId },
                        principalTable: "TemplateSectionQuestions",
                        principalColumns: new[] { "TemplateSectionRevisionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateSectionConditionalRules_TemplateSectionRevisions_Te~",
                        column: x => x.TemplateSectionRevisionId,
                        principalTable: "TemplateSectionRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TemplateQuestionCalculationReferences_ReferencedQuestionRe~1",
                table: "TemplateQuestionCalculationReferences",
                columns: new[] { "ReferencedQuestionRevisionId", "ReferencedQuestionId" });

            migrationBuilder.CreateIndex(
                name: "IX_TemplateSectionConditionalRules_TemplateSectionRevisionId_D~",
                table: "TemplateSectionConditionalRules",
                columns: new[] { "TemplateSectionRevisionId", "DependsOnSectionQuestionId" });

            migrationBuilder.CreateIndex(
                name: "IX_TemplateSectionConditionalRules_TemplateSectionRevisionId_T~",
                table: "TemplateSectionConditionalRules",
                columns: new[] { "TemplateSectionRevisionId", "TargetSectionQuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemplateSectionQuestions_TemplateQuestionId",
                table: "TemplateSectionQuestions",
                column: "TemplateQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateSectionQuestions_TemplateQuestionRevisionId_Templat~",
                table: "TemplateSectionQuestions",
                columns: new[] { "TemplateQuestionRevisionId", "TemplateQuestionId" });

            migrationBuilder.CreateIndex(
                name: "IX_TemplateSectionQuestions_TemplateSectionRevisionId_Order",
                table: "TemplateSectionQuestions",
                columns: new[] { "TemplateSectionRevisionId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemplateSectionQuestions_TemplateSectionRevisionId_Template~",
                table: "TemplateSectionQuestions",
                columns: new[] { "TemplateSectionRevisionId", "TemplateQuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemplateSectionRevisionAudits_ActorId",
                table: "TemplateSectionRevisionAudits",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateSectionRevisionAudits_CorrelationId",
                table: "TemplateSectionRevisionAudits",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateSectionRevisionAudits_TemplateSectionRevisionId_Occ~",
                table: "TemplateSectionRevisionAudits",
                columns: new[] { "TemplateSectionRevisionId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TemplateSectionRevisions_CreatedById",
                table: "TemplateSectionRevisions",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateSectionRevisions_LastDeletedById",
                table: "TemplateSectionRevisions",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateSectionRevisions_LastUpdatedById",
                table: "TemplateSectionRevisions",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateSectionRevisions_OneOpenRevision",
                table: "TemplateSectionRevisions",
                column: "TemplateSectionId",
                unique: true,
                filter: "\"Status\" IN (0, 1) AND \"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateSectionRevisions_OnePublishedRevision",
                table: "TemplateSectionRevisions",
                column: "TemplateSectionId",
                unique: true,
                filter: "\"Status\" = 2 AND \"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateSectionRevisions_PublishedById",
                table: "TemplateSectionRevisions",
                column: "PublishedById");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateSectionRevisions_ReviewedById",
                table: "TemplateSectionRevisions",
                column: "ReviewedById");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateSectionRevisions_TemplateSectionId_Sequence",
                table: "TemplateSectionRevisions",
                columns: new[] { "TemplateSectionId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemplateSections_CreatedById",
                table: "TemplateSections",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateSections_LastDeletedById",
                table: "TemplateSections",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateSections_LastUpdatedById",
                table: "TemplateSections",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateSections_TemplateAreaId_PurposeId_SubjectTypeId",
                table: "TemplateSections",
                columns: new[] { "TemplateAreaId", "PurposeId", "SubjectTypeId" });

            migrationBuilder.AddForeignKey(
                name: "FK_TemplateQuestionCalculationReferences_TemplateQuestionRevis~",
                table: "TemplateQuestionCalculationReferences",
                column: "TemplateQuestionRevisionId",
                principalTable: "TemplateQuestionRevisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TemplateQuestionCalculationReferences_TemplateQuestionRevi~1",
                table: "TemplateQuestionCalculationReferences",
                columns: new[] { "ReferencedQuestionRevisionId", "ReferencedQuestionId" },
                principalTable: "TemplateQuestionRevisions",
                principalColumns: new[] { "Id", "TemplateQuestionId" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TemplateQuestionCalculationReferences_TemplateQuestionRevis~",
                table: "TemplateQuestionCalculationReferences");

            migrationBuilder.DropForeignKey(
                name: "FK_TemplateQuestionCalculationReferences_TemplateQuestionRevi~1",
                table: "TemplateQuestionCalculationReferences");

            migrationBuilder.DropTable(
                name: "TemplateSectionConditionalRules");

            migrationBuilder.DropTable(
                name: "TemplateSectionRevisionAudits");

            migrationBuilder.DropTable(
                name: "TemplateSectionQuestions");

            migrationBuilder.DropTable(
                name: "TemplateSectionRevisions");

            migrationBuilder.DropTable(
                name: "TemplateSections");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_TemplateQuestionRevisions_Id_TemplateQuestionId",
                table: "TemplateQuestionRevisions");

            migrationBuilder.DropIndex(
                name: "IX_TemplateQuestionCalculationReferences_ReferencedQuestionRe~1",
                table: "TemplateQuestionCalculationReferences");

            migrationBuilder.AddForeignKey(
                name: "FK_TemplateQuestionCalculationReferences_TemplateQuestionRevis~",
                table: "TemplateQuestionCalculationReferences",
                column: "ReferencedQuestionRevisionId",
                principalTable: "TemplateQuestionRevisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TemplateQuestionCalculationReferences_TemplateQuestionRevi~1",
                table: "TemplateQuestionCalculationReferences",
                column: "TemplateQuestionRevisionId",
                principalTable: "TemplateQuestionRevisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
