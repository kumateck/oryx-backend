using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddFullProcedureFormRevisions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_TemplateSectionRevisions_Id_TemplateSectionId",
                table: "TemplateSectionRevisions",
                columns: new[] { "Id", "TemplateSectionId" });

            migrationBuilder.CreateTable(
                name: "TemplateForms",
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
                    table.PrimaryKey("PK_TemplateForms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplateForms_TemplateAreas_TemplateAreaId",
                        column: x => x.TemplateAreaId,
                        principalTable: "TemplateAreas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateForms_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TemplateForms_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TemplateForms_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TemplateFormRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateFormId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    RequiresEvidence = table.Column<bool>(type: "boolean", nullable: false),
                    RequiresSignature = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_TemplateFormRevisions", x => x.Id);
                    table.CheckConstraint("CK_TemplateFormRevision_ContentHash", "\"ContentHash\" ~ '^[a-f0-9]{64}$'");
                    table.CheckConstraint("CK_TemplateFormRevision_Sequence", "\"Sequence\" > 0");
                    table.CheckConstraint("CK_TemplateFormRevision_Status", "\"Status\" BETWEEN 0 AND 3");
                    table.ForeignKey(
                        name: "FK_TemplateFormRevisions_TemplateForms_TemplateFormId",
                        column: x => x.TemplateFormId,
                        principalTable: "TemplateForms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateFormRevisions_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TemplateFormRevisions_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TemplateFormRevisions_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TemplateFormRevisions_users_PublishedById",
                        column: x => x.PublishedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateFormRevisions_users_ReviewedById",
                        column: x => x.ReviewedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TemplateFormRevisionAudits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateFormRevisionId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_TemplateFormRevisionAudits", x => x.Id);
                    table.CheckConstraint("CK_TemplateFormRevisionAudit_ContentHash", "\"ContentHash\" ~ '^[a-f0-9]{64}$'");
                    table.CheckConstraint("CK_TemplateFormRevisionAudit_Status", "\"NewStatus\" BETWEEN 0 AND 3 AND (\"PriorStatus\" IS NULL OR \"PriorStatus\" BETWEEN 0 AND 3)");
                    table.ForeignKey(
                        name: "FK_TemplateFormRevisionAudits_TemplateFormRevisions_TemplateFo~",
                        column: x => x.TemplateFormRevisionId,
                        principalTable: "TemplateFormRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateFormRevisionAudits_users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TemplateFormSections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateFormRevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateSectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateSectionRevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateFormSections", x => x.Id);
                    table.UniqueConstraint("AK_TemplateFormSections_TemplateFormRevisionId_Id", x => new { x.TemplateFormRevisionId, x.Id });
                    table.CheckConstraint("CK_TemplateFormSection_Order", "\"Order\" >= 0");
                    table.ForeignKey(
                        name: "FK_TemplateFormSections_TemplateFormRevisions_TemplateFormRevi~",
                        column: x => x.TemplateFormRevisionId,
                        principalTable: "TemplateFormRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateFormSections_TemplateSectionRevisions_TemplateSecti~",
                        columns: x => new { x.TemplateSectionRevisionId, x.TemplateSectionId },
                        principalTable: "TemplateSectionRevisions",
                        principalColumns: new[] { "Id", "TemplateSectionId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateFormSections_TemplateSections_TemplateSectionId",
                        column: x => x.TemplateSectionId,
                        principalTable: "TemplateSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TemplateFormConditionalRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateFormRevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetFormSectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceFormSectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceQuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceQuestionRevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Operator = table.Column<int>(type: "integer", nullable: false),
                    ComparisonValue = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateFormConditionalRules", x => x.Id);
                    table.CheckConstraint("CK_TemplateFormConditionalRule_Operator", "\"Operator\" BETWEEN 0 AND 2");
                    table.CheckConstraint("CK_TemplateFormConditionalRule_Value", "(\"Operator\" = 2 AND \"ComparisonValue\" IS NULL) OR (\"Operator\" IN (0, 1) AND length(trim(\"ComparisonValue\")) > 0)");
                    table.ForeignKey(
                        name: "FK_TemplateFormConditionalRules_TemplateFormRevisions_Template~",
                        column: x => x.TemplateFormRevisionId,
                        principalTable: "TemplateFormRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateFormConditionalRules_TemplateFormSections_TemplateF~",
                        columns: x => new { x.TemplateFormRevisionId, x.SourceFormSectionId },
                        principalTable: "TemplateFormSections",
                        principalColumns: new[] { "TemplateFormRevisionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateFormConditionalRules_TemplateFormSections_Template~1",
                        columns: x => new { x.TemplateFormRevisionId, x.TargetFormSectionId },
                        principalTable: "TemplateFormSections",
                        principalColumns: new[] { "TemplateFormRevisionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateFormConditionalRules_TemplateQuestionRevisions_Sour~",
                        columns: x => new { x.SourceQuestionRevisionId, x.SourceQuestionId },
                        principalTable: "TemplateQuestionRevisions",
                        principalColumns: new[] { "Id", "TemplateQuestionId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TemplateFormConditionalRules_SourceQuestionRevisionId_Sourc~",
                table: "TemplateFormConditionalRules",
                columns: new[] { "SourceQuestionRevisionId", "SourceQuestionId" });

            migrationBuilder.CreateIndex(
                name: "IX_TemplateFormConditionalRules_TemplateFormRevisionId_SourceF~",
                table: "TemplateFormConditionalRules",
                columns: new[] { "TemplateFormRevisionId", "SourceFormSectionId" });

            migrationBuilder.CreateIndex(
                name: "IX_TemplateFormConditionalRules_TemplateFormRevisionId_TargetF~",
                table: "TemplateFormConditionalRules",
                columns: new[] { "TemplateFormRevisionId", "TargetFormSectionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemplateFormRevisionAudits_ActorId",
                table: "TemplateFormRevisionAudits",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateFormRevisionAudits_CorrelationId",
                table: "TemplateFormRevisionAudits",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateFormRevisionAudits_TemplateFormRevisionId_OccurredAt",
                table: "TemplateFormRevisionAudits",
                columns: new[] { "TemplateFormRevisionId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TemplateFormRevisions_CreatedById",
                table: "TemplateFormRevisions",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateFormRevisions_LastDeletedById",
                table: "TemplateFormRevisions",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateFormRevisions_LastUpdatedById",
                table: "TemplateFormRevisions",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateFormRevisions_OneOpenRevision",
                table: "TemplateFormRevisions",
                column: "TemplateFormId",
                unique: true,
                filter: "\"Status\" IN (0, 1) AND \"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateFormRevisions_OnePublishedRevision",
                table: "TemplateFormRevisions",
                column: "TemplateFormId",
                unique: true,
                filter: "\"Status\" = 2 AND \"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateFormRevisions_PublishedById",
                table: "TemplateFormRevisions",
                column: "PublishedById");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateFormRevisions_ReviewedById",
                table: "TemplateFormRevisions",
                column: "ReviewedById");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateFormRevisions_TemplateFormId_Sequence",
                table: "TemplateFormRevisions",
                columns: new[] { "TemplateFormId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemplateForms_CreatedById",
                table: "TemplateForms",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateForms_LastDeletedById",
                table: "TemplateForms",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateForms_LastUpdatedById",
                table: "TemplateForms",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateForms_TemplateAreaId_PurposeId_SubjectTypeId",
                table: "TemplateForms",
                columns: new[] { "TemplateAreaId", "PurposeId", "SubjectTypeId" });

            migrationBuilder.CreateIndex(
                name: "IX_TemplateFormSections_TemplateFormRevisionId_Order",
                table: "TemplateFormSections",
                columns: new[] { "TemplateFormRevisionId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemplateFormSections_TemplateFormRevisionId_TemplateSection~",
                table: "TemplateFormSections",
                columns: new[] { "TemplateFormRevisionId", "TemplateSectionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemplateFormSections_TemplateSectionId",
                table: "TemplateFormSections",
                column: "TemplateSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateFormSections_TemplateSectionRevisionId_TemplateSect~",
                table: "TemplateFormSections",
                columns: new[] { "TemplateSectionRevisionId", "TemplateSectionId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TemplateFormConditionalRules");

            migrationBuilder.DropTable(
                name: "TemplateFormRevisionAudits");

            migrationBuilder.DropTable(
                name: "TemplateFormSections");

            migrationBuilder.DropTable(
                name: "TemplateFormRevisions");

            migrationBuilder.DropTable(
                name: "TemplateForms");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_TemplateSectionRevisions_Id_TemplateSectionId",
                table: "TemplateSectionRevisions");
        }
    }
}
