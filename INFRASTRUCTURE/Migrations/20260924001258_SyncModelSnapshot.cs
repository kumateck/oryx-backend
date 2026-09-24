using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <summary>
    /// Snapshot resync. The QualityRoutines and Full Procedures migrations
    /// (20260915154318_AddRoutineQcAnalysis through 20260916234859_AddFullProcedureDefinitions)
    /// and the later QualityRoutines DbSet registration never updated
    /// ApplicationDbContextModelSnapshot, so every new migration tried to re-create ~57 tables.
    /// Each such table, column, index and foreign key was verified to already exist in an
    /// earlier migration, so none is repeated here.
    /// <para>
    /// The only schema-level change is naming: five index names and six foreign-key names in
    /// the Full Procedures migration exceed PostgreSQL's 63-character identifier limit. That
    /// migration used the full names, which PostgreSQL silently truncated to 63 characters,
    /// whereas EF's model truncates to 62 characters plus "~". The renames below align the
    /// database with the model so future EF operations on these objects resolve by name.
    /// </para>
    /// </summary>
    public partial class SyncModelSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE \"ProcedureRevisions\" RENAME CONSTRAINT \"FK_ProcedureRevisions_ProcedureDefinitions_ProcedureDefinitionI\" TO \"FK_ProcedureRevisions_ProcedureDefinitions_ProcedureDefinition~\";");
            migrationBuilder.Sql(
                "ALTER TABLE \"ProcedureRevisions\" RENAME CONSTRAINT \"FK_ProcedureRevisions_TemplateWorkflowRevisions_TemplateWorkflo\" TO \"FK_ProcedureRevisions_TemplateWorkflowRevisions_TemplateWorkfl~\";");
            migrationBuilder.Sql(
                "ALTER TABLE \"ProcedureApplicabilities\" RENAME CONSTRAINT \"FK_ProcedureApplicabilities_ProcedureRevisions_ProcedureRevisio\" TO \"FK_ProcedureApplicabilities_ProcedureRevisions_ProcedureRevisi~\";");
            migrationBuilder.Sql(
                "ALTER TABLE \"ProcedureRevisionAudits\" RENAME CONSTRAINT \"FK_ProcedureRevisionAudits_ProcedureRevisions_ProcedureRevision\" TO \"FK_ProcedureRevisionAudits_ProcedureRevisions_ProcedureRevisio~\";");
            migrationBuilder.Sql(
                "ALTER TABLE \"ProcedureStageScopes\" RENAME CONSTRAINT \"FK_ProcedureStageScopes_ProcedureRevisions_ProcedureRevisionId_\" TO \"FK_ProcedureStageScopes_ProcedureRevisions_ProcedureRevisionId~\";");
            migrationBuilder.Sql(
                "ALTER TABLE \"ProcedureStageScopes\" RENAME CONSTRAINT \"FK_ProcedureStageScopes_TemplateWorkflowNodes_TemplateWorkflowR\" TO \"FK_ProcedureStageScopes_TemplateWorkflowNodes_TemplateWorkflow~\";");

            migrationBuilder.RenameIndex(
                name: "IX_ProcedureApplicabilities_ProcedureRevisionId_ProductId_SiteI",
                table: "ProcedureApplicabilities",
                newName: "IX_ProcedureApplicabilities_ProcedureRevisionId_ProductId_Site~");

            migrationBuilder.RenameIndex(
                name: "IX_ProcedureRevisions_TemplateWorkflowRevisionId_TemplateWorkfl",
                table: "ProcedureRevisions",
                newName: "IX_ProcedureRevisions_TemplateWorkflowRevisionId_TemplateWorkf~");

            migrationBuilder.RenameIndex(
                name: "IX_ProcedureStageScopes_ProcedureRevisionId_TemplateWorkflowNod",
                table: "ProcedureStageScopes",
                newName: "IX_ProcedureStageScopes_ProcedureRevisionId_TemplateWorkflowNo~");

            migrationBuilder.RenameIndex(
                name: "IX_ProcedureStageScopes_ProcedureRevisionId_TemplateWorkflowRev",
                table: "ProcedureStageScopes",
                newName: "IX_ProcedureStageScopes_ProcedureRevisionId_TemplateWorkflowRe~");

            migrationBuilder.RenameIndex(
                name: "IX_ProcedureStageScopes_TemplateWorkflowRevisionId_TemplateWork",
                table: "ProcedureStageScopes",
                newName: "IX_ProcedureStageScopes_TemplateWorkflowRevisionId_TemplateWor~");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE \"ProcedureRevisions\" RENAME CONSTRAINT \"FK_ProcedureRevisions_ProcedureDefinitions_ProcedureDefinition~\" TO \"FK_ProcedureRevisions_ProcedureDefinitions_ProcedureDefinitionI\";");
            migrationBuilder.Sql(
                "ALTER TABLE \"ProcedureRevisions\" RENAME CONSTRAINT \"FK_ProcedureRevisions_TemplateWorkflowRevisions_TemplateWorkfl~\" TO \"FK_ProcedureRevisions_TemplateWorkflowRevisions_TemplateWorkflo\";");
            migrationBuilder.Sql(
                "ALTER TABLE \"ProcedureApplicabilities\" RENAME CONSTRAINT \"FK_ProcedureApplicabilities_ProcedureRevisions_ProcedureRevisi~\" TO \"FK_ProcedureApplicabilities_ProcedureRevisions_ProcedureRevisio\";");
            migrationBuilder.Sql(
                "ALTER TABLE \"ProcedureRevisionAudits\" RENAME CONSTRAINT \"FK_ProcedureRevisionAudits_ProcedureRevisions_ProcedureRevisio~\" TO \"FK_ProcedureRevisionAudits_ProcedureRevisions_ProcedureRevision\";");
            migrationBuilder.Sql(
                "ALTER TABLE \"ProcedureStageScopes\" RENAME CONSTRAINT \"FK_ProcedureStageScopes_ProcedureRevisions_ProcedureRevisionId~\" TO \"FK_ProcedureStageScopes_ProcedureRevisions_ProcedureRevisionId_\";");
            migrationBuilder.Sql(
                "ALTER TABLE \"ProcedureStageScopes\" RENAME CONSTRAINT \"FK_ProcedureStageScopes_TemplateWorkflowNodes_TemplateWorkflow~\" TO \"FK_ProcedureStageScopes_TemplateWorkflowNodes_TemplateWorkflowR\";");

            migrationBuilder.RenameIndex(
                name: "IX_ProcedureApplicabilities_ProcedureRevisionId_ProductId_Site~",
                table: "ProcedureApplicabilities",
                newName: "IX_ProcedureApplicabilities_ProcedureRevisionId_ProductId_SiteI");

            migrationBuilder.RenameIndex(
                name: "IX_ProcedureRevisions_TemplateWorkflowRevisionId_TemplateWorkf~",
                table: "ProcedureRevisions",
                newName: "IX_ProcedureRevisions_TemplateWorkflowRevisionId_TemplateWorkfl");

            migrationBuilder.RenameIndex(
                name: "IX_ProcedureStageScopes_ProcedureRevisionId_TemplateWorkflowNo~",
                table: "ProcedureStageScopes",
                newName: "IX_ProcedureStageScopes_ProcedureRevisionId_TemplateWorkflowNod");

            migrationBuilder.RenameIndex(
                name: "IX_ProcedureStageScopes_ProcedureRevisionId_TemplateWorkflowRe~",
                table: "ProcedureStageScopes",
                newName: "IX_ProcedureStageScopes_ProcedureRevisionId_TemplateWorkflowRev");

            migrationBuilder.RenameIndex(
                name: "IX_ProcedureStageScopes_TemplateWorkflowRevisionId_TemplateWor~",
                table: "ProcedureStageScopes",
                newName: "IX_ProcedureStageScopes_TemplateWorkflowRevisionId_TemplateWork");
        }
    }
}
