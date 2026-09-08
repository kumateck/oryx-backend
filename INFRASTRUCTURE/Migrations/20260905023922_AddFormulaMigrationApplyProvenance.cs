using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddFormulaMigrationApplyProvenance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $formula$
                BEGIN
                    IF EXISTS (SELECT 1 FROM "FormulaDefinitions") THEN
                        RAISE EXCEPTION 'Formula definition key migration requires an empty target table; run the controlled importer instead of inventing keys'
                            USING ERRCODE = '55000';
                    END IF;
                END;
                $formula$;
                """);

            migrationBuilder.AddColumn<string>(
                name: "ApplyManifestHash",
                table: "FormulaMigrationRuns",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedReportHash",
                table: "FormulaMigrationRuns",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ApprovalScope",
                table: "FormulaMigrationItems",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Key",
                table: "FormulaDefinitions",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddCheckConstraint(
                name: "CK_FormulaMigrationRun_ApplyManifestHash",
                table: "FormulaMigrationRuns",
                sql: "\"ApplyManifestHash\" IS NULL OR \"ApplyManifestHash\" ~ '^[a-f0-9]{64}$'");

            migrationBuilder.AddCheckConstraint(
                name: "CK_FormulaMigrationRun_SignedReportHash",
                table: "FormulaMigrationRuns",
                sql: "\"SignedReportHash\" IS NULL OR \"SignedReportHash\" ~ '^[a-f0-9]{64}$'");

            migrationBuilder.AddCheckConstraint(
                name: "CK_FormulaMigrationRun_ApplyProvenance",
                table: "FormulaMigrationRuns",
                sql: "\"Mode\" <> 1 OR (\"ApplyManifestHash\" IS NOT NULL AND \"SignedReportHash\" IS NOT NULL AND btrim(\"SignedReportLocation\") <> '')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_FormulaMigrationItem_ApprovalScope",
                table: "FormulaMigrationItems",
                sql: "\"ApprovalScope\" IS NULL OR \"ApprovalScope\" BETWEEN 0 AND 1");

            migrationBuilder.CreateIndex(
                name: "IX_FormulaDefinitions_Key",
                table: "FormulaDefinitions",
                column: "Key",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_FormulaDefinition_Key",
                table: "FormulaDefinitions",
                sql: "btrim(\"Key\") <> ''");

            migrationBuilder.Sql(MigrationRunGuard(includeSignedHash: true));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(MigrationRunGuard(includeSignedHash: false));

            migrationBuilder.DropCheckConstraint(
                name: "CK_FormulaMigrationRun_ApplyManifestHash",
                table: "FormulaMigrationRuns");

            migrationBuilder.DropCheckConstraint(
                name: "CK_FormulaMigrationRun_ApplyProvenance",
                table: "FormulaMigrationRuns");

            migrationBuilder.DropCheckConstraint(
                name: "CK_FormulaMigrationRun_SignedReportHash",
                table: "FormulaMigrationRuns");

            migrationBuilder.DropCheckConstraint(
                name: "CK_FormulaMigrationItem_ApprovalScope",
                table: "FormulaMigrationItems");

            migrationBuilder.DropIndex(
                name: "IX_FormulaDefinitions_Key",
                table: "FormulaDefinitions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_FormulaDefinition_Key",
                table: "FormulaDefinitions");

            migrationBuilder.DropColumn(
                name: "ApplyManifestHash",
                table: "FormulaMigrationRuns");

            migrationBuilder.DropColumn(
                name: "SignedReportHash",
                table: "FormulaMigrationRuns");

            migrationBuilder.DropColumn(
                name: "ApprovalScope",
                table: "FormulaMigrationItems");

            migrationBuilder.DropColumn(
                name: "Key",
                table: "FormulaDefinitions");
        }

        private static string MigrationRunGuard(bool includeSignedHash)
        {
            var applyProvenanceGuard = includeSignedHash
                ? """
                  OR OLD."ApplyManifestHash" IS DISTINCT FROM NEW."ApplyManifestHash"
                  OR OLD."SignedReportHash" IS DISTINCT FROM NEW."SignedReportHash"
                  OR OLD."SignedReportLocation" IS DISTINCT FROM NEW."SignedReportLocation"
                  """
                : string.Empty;
            return $$"""
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
                        {{applyProvenanceGuard}}
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
                """;
        }
    }
}
