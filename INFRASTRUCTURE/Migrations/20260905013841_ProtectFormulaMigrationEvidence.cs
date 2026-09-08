using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class ProtectFormulaMigrationEvidence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TRIGGER "TR_FormulaMigrationItems_AppendOnly"
                    BEFORE UPDATE OR DELETE ON "FormulaMigrationItems"
                    FOR EACH ROW EXECUTE FUNCTION oryx_formula_v1_reject_mutation();

                CREATE OR REPLACE FUNCTION oryx_formula_v1_guard_migration_run()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $formula$
                BEGIN
                    IF TG_OP = 'DELETE' THEN
                        RAISE EXCEPTION 'Formula migration runs cannot be deleted'
                            USING ERRCODE = '55000';
                    END IF;

                    IF OLD."Mode" = 0 OR OLD."Status" IN (2, 3) THEN
                        RAISE EXCEPTION 'Completed, failed, and dry-run migration evidence is immutable'
                            USING ERRCODE = '55000';
                    END IF;

                    IF OLD."ReleaseId" IS DISTINCT FROM NEW."ReleaseId"
                        OR OLD."SourceFingerprint" IS DISTINCT FROM NEW."SourceFingerprint"
                        OR OLD."CorpusChecksum" IS DISTINCT FROM NEW."CorpusChecksum"
                        OR OLD."CodeVersion" IS DISTINCT FROM NEW."CodeVersion"
                        OR OLD."Mode" IS DISTINCT FROM NEW."Mode"
                        OR OLD."InitiatedById" IS DISTINCT FROM NEW."InitiatedById"
                        OR OLD."StartedAt" IS DISTINCT FROM NEW."StartedAt" THEN
                        RAISE EXCEPTION 'Formula migration run identity and provenance are immutable'
                            USING ERRCODE = '55000';
                    END IF;

                    IF OLD."Status" IS DISTINCT FROM NEW."Status" AND NOT (
                        (OLD."Status" = 0 AND NEW."Status" = 1)
                        OR (OLD."Status" = 1 AND NEW."Status" IN (2, 3))
                    ) THEN
                        RAISE EXCEPTION 'Illegal formula migration run status transition'
                            USING ERRCODE = '55000';
                    END IF;

                    IF NEW."Status" IN (2, 3) AND NEW."CompletedAt" IS NULL THEN
                        RAISE EXCEPTION 'A terminal formula migration run requires a completion time'
                            USING ERRCODE = '55000';
                    END IF;
                    IF NEW."Status" IN (0, 1) AND NEW."CompletedAt" IS NOT NULL THEN
                        RAISE EXCEPTION 'A non-terminal formula migration run cannot have a completion time'
                            USING ERRCODE = '55000';
                    END IF;
                    RETURN NEW;
                END;
                $formula$;

                CREATE TRIGGER "TR_FormulaMigrationRuns_Guard"
                    BEFORE UPDATE OR DELETE ON "FormulaMigrationRuns"
                    FOR EACH ROW EXECUTE FUNCTION oryx_formula_v1_guard_migration_run();
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS "TR_FormulaMigrationRuns_Guard"
                    ON "FormulaMigrationRuns";
                DROP FUNCTION IF EXISTS oryx_formula_v1_guard_migration_run();
                DROP TRIGGER IF EXISTS "TR_FormulaMigrationItems_AppendOnly"
                    ON "FormulaMigrationItems";
                """
            );
        }
    }
}
