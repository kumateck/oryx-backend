using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations;

public partial class AddFullProcedureDefinitions
{
    private static void CreateDetailTables(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ProcedureApplicabilities",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ProcedureRevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                SiteId = table.Column<Guid>(type: "uuid", nullable: false),
                BatchType = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProcedureApplicabilities", x => x.Id);
                table.CheckConstraint("CK_ProcedureApplicability_BatchType", "\"BatchType\" BETWEEN 0 AND 3");
                table.ForeignKey("FK_ProcedureApplicabilities_ProcedureRevisions_ProcedureRevisionId",
                    x => x.ProcedureRevisionId, "ProcedureRevisions", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_ProcedureApplicabilities_Products_ProductId",
                    x => x.ProductId, "Products", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_ProcedureApplicabilities_Sites_SiteId",
                    x => x.SiteId, "Sites", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "ProcedureRevisionAudits",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ProcedureRevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                PriorStatus = table.Column<int>(type: "integer", nullable: true),
                NewStatus = table.Column<int>(type: "integer", nullable: false),
                Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Reason = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                SnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CorrelationId = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProcedureRevisionAudits", x => x.Id);
                table.CheckConstraint("CK_ProcedureRevisionAudit_ContentHash", "\"ContentHash\" ~ '^[a-f0-9]{64}$'");
                table.CheckConstraint("CK_ProcedureRevisionAudit_Status", "\"NewStatus\" BETWEEN 0 AND 3 AND (\"PriorStatus\" IS NULL OR \"PriorStatus\" BETWEEN 0 AND 3)");
                table.ForeignKey("FK_ProcedureRevisionAudits_ProcedureRevisions_ProcedureRevisionId",
                    x => x.ProcedureRevisionId, "ProcedureRevisions", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_ProcedureRevisionAudits_users_ActorId",
                    x => x.ActorId, "users", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "ProcedureStageScopes",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ProcedureRevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                TemplateWorkflowRevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                TemplateWorkflowNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                WorkflowNodeKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                WorkflowNodeName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                WorkflowNodeOrder = table.Column<int>(type: "integer", nullable: false),
                RecordScope = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProcedureStageScopes", x => x.Id);
                table.CheckConstraint("CK_ProcedureStageScope_RecordScope", "\"RecordScope\" BETWEEN 0 AND 3");
                table.ForeignKey("FK_ProcedureStageScopes_ProcedureRevisions_ProcedureRevisionId_TemplateWorkflowRevisionId",
                    x => new { x.ProcedureRevisionId, x.TemplateWorkflowRevisionId },
                    "ProcedureRevisions", new[] { "Id", "TemplateWorkflowRevisionId" },
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_ProcedureStageScopes_TemplateWorkflowNodes_TemplateWorkflowRevisionId_TemplateWorkflowNodeId",
                    x => new { x.TemplateWorkflowRevisionId, x.TemplateWorkflowNodeId },
                    "TemplateWorkflowNodes", new[] { "TemplateWorkflowRevisionId", "Id" },
                    onDelete: ReferentialAction.Restrict);
            });
    }
}
