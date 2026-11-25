using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace INFRASTRUCTURE.Migrations;

/// <summary>Restore the status-only, deferred audit check on existing customer databases.</summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20261125110000_RepairFormulaRevisionStatusAuditTrigger")]
public sealed class RepairFormulaRevisionStatusAuditTrigger : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql(AuditSql(skipUnchanged: true));

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql(AuditSql(skipUnchanged: false));

    private static string AuditSql(bool skipUnchanged) => $$"""
        CREATE OR REPLACE FUNCTION public.oryx_formula_v1_require_revision_audit()
        RETURNS trigger
        LANGUAGE plpgsql
        AS $formula$
        BEGIN
            {{(skipUnchanged ? "IF OLD.\"Status\" IS NOT DISTINCT FROM NEW.\"Status\" THEN RETURN NULL; END IF;" : "")}}
            IF NOT EXISTS (
                SELECT 1
                FROM public."FormulaRevisionAudits" audit
                WHERE audit."FormulaRevisionId" = NEW."Id"
                  AND audit."PriorStatus" = OLD."Status"
                  AND audit."NewStatus" = NEW."Status"
                  AND audit."DefinitionHash" = NEW."DefinitionHash"
                  AND audit."ActorId" <> '00000000-0000-0000-0000-000000000000'::uuid
                  AND audit."CorrelationId" <> '00000000-0000-0000-0000-000000000000'::uuid
                  AND btrim(audit."Reason") <> ''
                  AND audit.xmin::text::bigint = txid_current()
            ) THEN
                RAISE EXCEPTION 'Formula revision status transition requires a matching audit row in the same transaction'
                    USING ERRCODE = '55000';
            END IF;
            RETURN NULL;
        END;
        $formula$;

        DROP TRIGGER IF EXISTS "TR_FormulaRevisions_StatusAudit" ON public."FormulaRevisions";
        CREATE CONSTRAINT TRIGGER "TR_FormulaRevisions_StatusAudit"
            AFTER UPDATE ON public."FormulaRevisions"
            DEFERRABLE INITIALLY DEFERRED
            FOR EACH ROW
            WHEN (OLD."Status" IS DISTINCT FROM NEW."Status")
            EXECUTE FUNCTION public.oryx_formula_v1_require_revision_audit();
        """;
}
