using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations;

public partial class AddFullProcedureDefinitions
{
    private static void CreateIndexes(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex("IX_ProcedureApplicabilities_ProcedureRevisionId_ProductId_SiteId_BatchType",
            "ProcedureApplicabilities", new[] { "ProcedureRevisionId", "ProductId", "SiteId", "BatchType" }, unique: true);
        migrationBuilder.CreateIndex("IX_ProcedureApplicabilities_ProductId",
            "ProcedureApplicabilities", "ProductId");
        migrationBuilder.CreateIndex("IX_ProcedureApplicabilities_SiteId",
            "ProcedureApplicabilities", "SiteId");
        migrationBuilder.CreateIndex("IX_ProcedureDefinitions_CreatedById",
            "ProcedureDefinitions", "CreatedById");
        migrationBuilder.CreateIndex("IX_ProcedureDefinitions_LastDeletedById",
            "ProcedureDefinitions", "LastDeletedById");
        migrationBuilder.CreateIndex("IX_ProcedureDefinitions_LastUpdatedById",
            "ProcedureDefinitions", "LastUpdatedById");
        migrationBuilder.CreateIndex("IX_ProcedureDefinitions_TemplateAreaId_PurposeId_SubjectTypeId",
            "ProcedureDefinitions", new[] { "TemplateAreaId", "PurposeId", "SubjectTypeId" });
        migrationBuilder.CreateIndex("IX_ProcedureRevisionAudits_ActorId",
            "ProcedureRevisionAudits", "ActorId");
        migrationBuilder.CreateIndex("IX_ProcedureRevisionAudits_CorrelationId",
            "ProcedureRevisionAudits", "CorrelationId");
        migrationBuilder.CreateIndex("IX_ProcedureRevisionAudits_ProcedureRevisionId_OccurredAt",
            "ProcedureRevisionAudits", new[] { "ProcedureRevisionId", "OccurredAt" });
        migrationBuilder.CreateIndex("IX_ProcedureRevisions_ApprovedById",
            "ProcedureRevisions", "ApprovedById");
        migrationBuilder.CreateIndex("IX_ProcedureRevisions_CreatedById",
            "ProcedureRevisions", "CreatedById");
        migrationBuilder.CreateIndex("IX_ProcedureRevisions_LastDeletedById",
            "ProcedureRevisions", "LastDeletedById");
        migrationBuilder.CreateIndex("IX_ProcedureRevisions_LastUpdatedById",
            "ProcedureRevisions", "LastUpdatedById");
        migrationBuilder.CreateIndex("IX_ProcedureRevisions_OneApprovedRevision",
            "ProcedureRevisions", "ProcedureDefinitionId", unique: true,
            filter: "\"Status\" = 2 AND \"DeletedAt\" IS NULL");
        migrationBuilder.CreateIndex("IX_ProcedureRevisions_OneOpenRevision",
            "ProcedureRevisions", "ProcedureDefinitionId", unique: true,
            filter: "\"Status\" IN (0, 1) AND \"DeletedAt\" IS NULL");
        migrationBuilder.CreateIndex("IX_ProcedureRevisions_ProcedureDefinitionId_Sequence",
            "ProcedureRevisions", new[] { "ProcedureDefinitionId", "Sequence" }, unique: true);
        migrationBuilder.CreateIndex("IX_ProcedureRevisions_ReviewedById",
            "ProcedureRevisions", "ReviewedById");
        migrationBuilder.CreateIndex("IX_ProcedureRevisions_TemplateWorkflowRevisionId_TemplateWorkflowId",
            "ProcedureRevisions", new[] { "TemplateWorkflowRevisionId", "TemplateWorkflowId" });
        migrationBuilder.CreateIndex("IX_ProcedureStageScopes_ProcedureRevisionId_TemplateWorkflowNodeId",
            "ProcedureStageScopes", new[] { "ProcedureRevisionId", "TemplateWorkflowNodeId" }, unique: true);
        migrationBuilder.CreateIndex("IX_ProcedureStageScopes_ProcedureRevisionId_TemplateWorkflowRevisionId",
            "ProcedureStageScopes", new[] { "ProcedureRevisionId", "TemplateWorkflowRevisionId" });
        migrationBuilder.CreateIndex("IX_ProcedureStageScopes_TemplateWorkflowRevisionId_TemplateWorkflowNodeId",
            "ProcedureStageScopes", new[] { "TemplateWorkflowRevisionId", "TemplateWorkflowNodeId" });
    }
}
