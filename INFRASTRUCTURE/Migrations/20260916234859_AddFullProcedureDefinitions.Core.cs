using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations;

public partial class AddFullProcedureDefinitions
{
    private static void CreateCoreTables(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ProcedureDefinitions",
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
                LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProcedureDefinitions", x => x.Id);
                table.ForeignKey("FK_ProcedureDefinitions_TemplateAreas_TemplateAreaId",
                    x => x.TemplateAreaId, "TemplateAreas", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_ProcedureDefinitions_users_CreatedById",
                    x => x.CreatedById, "users", "Id");
                table.ForeignKey("FK_ProcedureDefinitions_users_LastDeletedById",
                    x => x.LastDeletedById, "users", "Id");
                table.ForeignKey("FK_ProcedureDefinitions_users_LastUpdatedById",
                    x => x.LastUpdatedById, "users", "Id");
            });

        migrationBuilder.CreateTable(
            name: "ProcedureRevisions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ProcedureDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                Sequence = table.Column<int>(type: "integer", nullable: false),
                Status = table.Column<int>(type: "integer", nullable: false),
                Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                TemplateWorkflowId = table.Column<Guid>(type: "uuid", nullable: false),
                TemplateWorkflowRevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                TemplateWorkflowName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                TemplateWorkflowContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                ParameterSchemaJson = table.Column<string>(type: "jsonb", nullable: false),
                ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                ReviewedById = table.Column<Guid>(type: "uuid", nullable: true),
                ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                ApprovedById = table.Column<Guid>(type: "uuid", nullable: true),
                ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                RetiredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProcedureRevisions", x => x.Id);
                table.UniqueConstraint("AK_ProcedureRevisions_Id_TemplateWorkflowRevisionId",
                    x => new { x.Id, x.TemplateWorkflowRevisionId });
                table.CheckConstraint("CK_ProcedureRevision_ContentHash", "\"ContentHash\" ~ '^[a-f0-9]{64}$'");
                table.CheckConstraint("CK_ProcedureRevision_Sequence", "\"Sequence\" > 0");
                table.CheckConstraint("CK_ProcedureRevision_Status", "\"Status\" BETWEEN 0 AND 3");
                table.CheckConstraint("CK_ProcedureRevision_WorkflowHash", "\"TemplateWorkflowContentHash\" ~ '^[a-f0-9]{64}$'");
                table.ForeignKey("FK_ProcedureRevisions_ProcedureDefinitions_ProcedureDefinitionId",
                    x => x.ProcedureDefinitionId, "ProcedureDefinitions", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_ProcedureRevisions_TemplateWorkflowRevisions_TemplateWorkflowRevisionId_TemplateWorkflowId",
                    x => new { x.TemplateWorkflowRevisionId, x.TemplateWorkflowId },
                    "TemplateWorkflowRevisions", new[] { "Id", "TemplateWorkflowId" },
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_ProcedureRevisions_users_ApprovedById",
                    x => x.ApprovedById, "users", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_ProcedureRevisions_users_CreatedById",
                    x => x.CreatedById, "users", "Id");
                table.ForeignKey("FK_ProcedureRevisions_users_LastDeletedById",
                    x => x.LastDeletedById, "users", "Id");
                table.ForeignKey("FK_ProcedureRevisions_users_LastUpdatedById",
                    x => x.LastUpdatedById, "users", "Id");
                table.ForeignKey("FK_ProcedureRevisions_users_ReviewedById",
                    x => x.ReviewedById, "users", "Id", onDelete: ReferentialAction.Restrict);
            });
    }
}
