using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddFormulaV1IntegrityTriggers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "FormulaRevisionAudits"
                    ADD CONSTRAINT "CK_FormulaRevisionAudit_DefinitionHash"
                    CHECK ("DefinitionHash" ~ '^[a-f0-9]{64}$'),
                    ADD CONSTRAINT "CK_FormulaRevisionAudit_Statuses"
                    CHECK (
                        ("PriorStatus" IS NULL OR "PriorStatus" BETWEEN 0 AND 3)
                        AND "NewStatus" BETWEEN 0 AND 3
                    ),
                    ADD CONSTRAINT "CK_FormulaRevisionAudit_Action"
                    CHECK (btrim("Action") <> ''),
                    ADD CONSTRAINT "CK_FormulaRevisionAudit_CorrelationId"
                    CHECK ("CorrelationId" <> '00000000-0000-0000-0000-000000000000'::uuid),
                    ADD CONSTRAINT "CK_FormulaRevisionAudit_Reason"
                    CHECK (btrim("Reason") <> '');

                ALTER TABLE "FormFieldRevisions"
                    ADD CONSTRAINT "CK_FormFieldRevision_FieldHash"
                    CHECK ("FieldHash" ~ '^[a-f0-9]{64}$');

                ALTER TABLE "FormulaExecutions"
                    ADD CONSTRAINT "CK_FormulaExecution_EngineBuildHash"
                    CHECK ("EngineBuildHash" ~ '^[a-f0-9]{64}$');

                ALTER TABLE "FormulaMigrationRuns"
                    ADD CONSTRAINT "CK_FormulaMigrationRun_CorpusChecksum"
                    CHECK ("CorpusChecksum" ~ '^[a-f0-9]{64}$'),
                    ADD CONSTRAINT "CK_FormulaMigrationRun_CountBalance"
                    CHECK ("SucceededCount" + "FailedCount" <= "TotalCount");

                ALTER TABLE "ResponseFormulaSubmissionSets"
                    ADD CONSTRAINT "CK_ResponseFormulaSubmissionSet_Hashes"
                    CHECK (
                        "SetHash" ~ '^[a-f0-9]{64}$'
                        AND "InputAggregateHash" ~ '^[a-f0-9]{64}$'
                        AND "ConfigurationAggregateHash" ~ '^[a-f0-9]{64}$'
                    );

                CREATE OR REPLACE FUNCTION oryx_formula_v1_reject_mutation()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $formula$
                BEGIN
                    RAISE EXCEPTION '% is append-only; insert a superseding record', TG_TABLE_NAME
                        USING ERRCODE = '55000';
                END;
                $formula$;

                DO $formula$
                DECLARE
                    target_table text;
                BEGIN
                    FOREACH target_table IN ARRAY ARRAY[
                        'FormulaRevisionAudits',
                        'LegacyFormulaArtifacts',
                        'LegacyKeyMappings',
                        'ResponseFormulaSnapshots',
                        'FormulaExecutions',
                        'ResponseFormulaSubmissionSets',
                        'ResponseFormulaSubmissionExecutions',
                        'FormulaReconciliationResults'
                    ]
                    LOOP
                        EXECUTE format(
                            'CREATE TRIGGER %I BEFORE UPDATE OR DELETE ON %I '
                            || 'FOR EACH ROW EXECUTE FUNCTION oryx_formula_v1_reject_mutation()',
                            'TR_' || target_table || '_AppendOnly',
                            target_table
                        );
                    END LOOP;
                END;
                $formula$;

                CREATE OR REPLACE FUNCTION oryx_formula_v1_guard_revision()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $formula$
                BEGIN
                    IF TG_OP = 'INSERT' THEN
                        IF NEW."Status" <> 0 THEN
                            RAISE EXCEPTION 'A formula revision must be created as Draft'
                                USING ERRCODE = '55000';
                        END IF;
                        RETURN NEW;
                    END IF;

                    IF TG_OP = 'DELETE' THEN
                        RAISE EXCEPTION 'Formula revisions must be retired, never deleted'
                            USING ERRCODE = '55000';
                    END IF;

                    IF OLD."Status" <> 0 AND (
                        OLD."DefinitionJson" IS DISTINCT FROM NEW."DefinitionJson"
                        OR OLD."TestCasesJson" IS DISTINCT FROM NEW."TestCasesJson"
                        OR OLD."DefinitionHash" IS DISTINCT FROM NEW."DefinitionHash"
                        OR OLD."ReleaseEvidenceHash" IS DISTINCT FROM NEW."ReleaseEvidenceHash"
                        OR OLD."FormulaLanguageVersion" IS DISTINCT FROM NEW."FormulaLanguageVersion"
                        OR OLD."NumericPolicyVersion" IS DISTINCT FROM NEW."NumericPolicyVersion"
                    ) THEN
                        RAISE EXCEPTION 'Formula definition and release evidence are immutable after review starts'
                            USING ERRCODE = '55000';
                    END IF;

                    IF OLD."Status" IS DISTINCT FROM NEW."Status" AND NOT (
                        (OLD."Status" = 0 AND NEW."Status" = 1)
                        OR (OLD."Status" = 1 AND NEW."Status" = 0)
                        OR (OLD."Status" = 1 AND NEW."Status" = 2)
                        OR (OLD."Status" = 2 AND NEW."Status" = 3)
                    ) THEN
                        RAISE EXCEPTION 'Illegal formula revision status transition: % -> %',
                            OLD."Status", NEW."Status" USING ERRCODE = '55000';
                    END IF;

                    IF NEW."Status" = 2 AND (
                        NEW."ApprovedById" IS NULL
                        OR NEW."ApprovedAt" IS NULL
                        OR NEW."EffectiveAt" IS NULL
                    ) THEN
                        RAISE EXCEPTION 'An approved formula revision requires approver, approval time, and effective time'
                            USING ERRCODE = '55000';
                    END IF;
                    IF NEW."Status" = 3 AND NEW."RetiredAt" IS NULL THEN
                        RAISE EXCEPTION 'A retired formula revision requires a retirement time'
                            USING ERRCODE = '55000';
                    END IF;

                    RETURN NEW;
                END;
                $formula$;

                CREATE TRIGGER "TR_FormulaRevisions_Guard"
                    BEFORE INSERT OR UPDATE OR DELETE ON "FormulaRevisions"
                    FOR EACH ROW EXECUTE FUNCTION oryx_formula_v1_guard_revision();

                CREATE OR REPLACE FUNCTION oryx_formula_v1_require_revision_audit()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $formula$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM "FormulaRevisionAudits" audit
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

                CREATE CONSTRAINT TRIGGER "TR_FormulaRevisions_StatusAudit"
                    AFTER UPDATE ON "FormulaRevisions"
                    DEFERRABLE INITIALLY DEFERRED
                    FOR EACH ROW
                    WHEN (OLD."Status" IS DISTINCT FROM NEW."Status")
                    EXECUTE FUNCTION oryx_formula_v1_require_revision_audit();

                CREATE OR REPLACE FUNCTION oryx_formula_v1_guard_form_revision()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $formula$
                BEGIN
                    IF TG_OP = 'INSERT' THEN
                        IF NEW."Status" <> 0 THEN
                            RAISE EXCEPTION 'A form revision must be created as Draft'
                                USING ERRCODE = '55000';
                        END IF;
                        RETURN NEW;
                    END IF;
                    IF TG_OP = 'DELETE' THEN
                        RAISE EXCEPTION 'Form revisions must be retired, never deleted'
                            USING ERRCODE = '55000';
                    END IF;
                    IF OLD."Status" <> 0 AND OLD."ContentHash" IS DISTINCT FROM NEW."ContentHash" THEN
                        RAISE EXCEPTION 'Form revision content is immutable after review starts'
                            USING ERRCODE = '55000';
                    END IF;
                    IF OLD."Status" IS DISTINCT FROM NEW."Status" AND NOT (
                        (OLD."Status" = 0 AND NEW."Status" = 1)
                        OR (OLD."Status" = 1 AND NEW."Status" = 0)
                        OR (OLD."Status" = 1 AND NEW."Status" = 2)
                        OR (OLD."Status" = 2 AND NEW."Status" = 3)
                    ) THEN
                        RAISE EXCEPTION 'Illegal form revision status transition: % -> %',
                            OLD."Status", NEW."Status" USING ERRCODE = '55000';
                    END IF;
                    IF NEW."Status" = 2 AND (
                        NEW."ApprovedById" IS NULL OR NEW."ApprovedAt" IS NULL
                    ) THEN
                        RAISE EXCEPTION 'An approved form revision requires approver and approval time'
                            USING ERRCODE = '55000';
                    END IF;
                    RETURN NEW;
                END;
                $formula$;

                CREATE TRIGGER "TR_FormRevisions_Guard"
                    BEFORE INSERT OR UPDATE OR DELETE ON "FormRevisions"
                    FOR EACH ROW EXECUTE FUNCTION oryx_formula_v1_guard_form_revision();

                CREATE OR REPLACE FUNCTION oryx_formula_v1_guard_form_field_revision()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $formula$
                DECLARE
                    parent_status integer;
                    parent_id uuid;
                BEGIN
                    parent_id := CASE WHEN TG_OP = 'DELETE'
                        THEN OLD."FormRevisionId" ELSE NEW."FormRevisionId" END;
                    SELECT "Status" INTO parent_status FROM "FormRevisions" WHERE "Id" = parent_id;
                    IF NOT FOUND OR parent_status <> 0 THEN
                        RAISE EXCEPTION 'Form fields may change only while their form revision is Draft'
                            USING ERRCODE = '55000';
                    END IF;
                    IF TG_OP = 'DELETE' THEN
                        RETURN OLD;
                    END IF;
                    RETURN NEW;
                END;
                $formula$;

                CREATE TRIGGER "TR_FormFieldRevisions_ParentDraft"
                    BEFORE INSERT OR UPDATE OR DELETE ON "FormFieldRevisions"
                    FOR EACH ROW EXECUTE FUNCTION oryx_formula_v1_guard_form_field_revision();

                CREATE OR REPLACE FUNCTION oryx_formula_v1_guard_formula_configuration()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $formula$
                DECLARE
                    parent_status integer;
                    field_revision_id uuid;
                BEGIN
                    field_revision_id := CASE WHEN TG_OP = 'DELETE'
                        THEN OLD."FormFieldRevisionId" ELSE NEW."FormFieldRevisionId" END;
                    SELECT form_revision."Status" INTO parent_status
                    FROM "FormFieldRevisions" field_revision
                    JOIN "FormRevisions" form_revision
                      ON form_revision."Id" = field_revision."FormRevisionId"
                    WHERE field_revision."Id" = field_revision_id;
                    IF NOT FOUND OR parent_status <> 0 THEN
                        RAISE EXCEPTION 'Formula bindings may change only while their form revision is Draft'
                            USING ERRCODE = '55000';
                    END IF;
                    IF TG_OP = 'DELETE' THEN
                        RETURN OLD;
                    END IF;
                    RETURN NEW;
                END;
                $formula$;

                CREATE TRIGGER "TR_FormFieldFormulaConfigurations_ParentDraft"
                    BEFORE INSERT OR UPDATE OR DELETE ON "FormFieldFormulaConfigurations"
                    FOR EACH ROW EXECUTE FUNCTION oryx_formula_v1_guard_formula_configuration();

                CREATE OR REPLACE FUNCTION oryx_formula_v1_guard_snapshot()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $formula$
                DECLARE
                    prior_record record;
                    revision_record record;
                BEGIN
                    IF NEW."Sequence" = 1 THEN
                        IF NEW."SupersedesSnapshotId" IS NOT NULL OR NEW."Reason" <> 0 THEN
                            RAISE EXCEPTION 'Initial snapshot must use sequence 1, reason Initial, and no predecessor'
                                USING ERRCODE = '55000';
                        END IF;
                    ELSE
                        IF NEW."SupersedesSnapshotId" IS NULL OR NEW."Reason" = 0 THEN
                            RAISE EXCEPTION 'Later snapshot generations require a predecessor and non-initial reason'
                                USING ERRCODE = '55000';
                        END IF;
                        IF NEW."ApprovalReference" IS NULL OR btrim(NEW."ApprovalReference") = '' THEN
                            RAISE EXCEPTION 'Rebase and correction snapshots require an approval reference'
                                USING ERRCODE = '55000';
                        END IF;

                        SELECT "ResponseId", "PlacementKey", "Sequence"
                        INTO prior_record
                        FROM "ResponseFormulaSnapshots"
                        WHERE "Id" = NEW."SupersedesSnapshotId";
                        IF NOT FOUND OR prior_record."ResponseId" <> NEW."ResponseId"
                           OR prior_record."PlacementKey" <> NEW."PlacementKey"
                           OR prior_record."Sequence" <> NEW."Sequence" - 1 THEN
                            RAISE EXCEPTION 'Snapshot must supersede the immediately preceding generation for the same placement'
                                USING ERRCODE = '55000';
                        END IF;
                    END IF;

                    SELECT "Status", "DefinitionHash"
                    INTO revision_record
                    FROM "FormulaRevisions"
                    WHERE "Id" = NEW."FormulaRevisionId";
                    IF NOT FOUND OR revision_record."Status" <> 2
                       OR revision_record."DefinitionHash" <> NEW."DefinitionHash" THEN
                        RAISE EXCEPTION 'Snapshot requires an approved formula revision with the same definition hash'
                            USING ERRCODE = '55000';
                    END IF;
                    RETURN NEW;
                END;
                $formula$;

                CREATE TRIGGER "TR_ResponseFormulaSnapshots_Integrity"
                    BEFORE INSERT ON "ResponseFormulaSnapshots"
                    FOR EACH ROW EXECUTE FUNCTION oryx_formula_v1_guard_snapshot();

                CREATE OR REPLACE FUNCTION oryx_formula_v1_guard_execution()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $formula$
                DECLARE
                    current_snapshot record;
                    prior_record record;
                BEGIN
                    IF NEW."Status" = 0 AND (
                        NEW."ResultHash" IS NULL
                        OR NEW."RawResultsJson" IS NULL
                        OR NEW."RoundedResultsJson" IS NULL
                        OR NEW."DisplayResultsJson" IS NULL
                    ) THEN
                        RAISE EXCEPTION 'A valid formula execution requires hashed raw, rounded, and display results'
                            USING ERRCODE = '55000';
                    END IF;

                    IF NEW."SupersedesExecutionId" IS NULL THEN
                        RETURN NEW;
                    END IF;
                    IF NEW."SupersedesExecutionId" = NEW."Id" THEN
                        RAISE EXCEPTION 'A formula execution cannot supersede itself'
                            USING ERRCODE = '55000';
                    END IF;

                    SELECT "ResponseId", "PlacementKey"
                    INTO current_snapshot
                    FROM "ResponseFormulaSnapshots"
                    WHERE "Id" = NEW."ResponseFormulaSnapshotId";

                    SELECT snapshot."ResponseId", snapshot."PlacementKey", execution."ExecutedAt"
                    INTO prior_record
                    FROM "FormulaExecutions" execution
                    JOIN "ResponseFormulaSnapshots" snapshot
                      ON snapshot."Id" = execution."ResponseFormulaSnapshotId"
                    WHERE execution."Id" = NEW."SupersedesExecutionId";

                    IF NOT FOUND OR prior_record."ResponseId" <> current_snapshot."ResponseId"
                       OR prior_record."PlacementKey" <> current_snapshot."PlacementKey"
                       OR prior_record."ExecutedAt" > NEW."ExecutedAt" THEN
                        RAISE EXCEPTION 'Execution predecessor must be an earlier execution for the same response placement'
                            USING ERRCODE = '55000';
                    END IF;
                    RETURN NEW;
                END;
                $formula$;

                CREATE TRIGGER "TR_FormulaExecutions_Integrity"
                    BEFORE INSERT ON "FormulaExecutions"
                    FOR EACH ROW EXECUTE FUNCTION oryx_formula_v1_guard_execution();

                CREATE OR REPLACE FUNCTION oryx_formula_v1_guard_submission_member()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $formula$
                DECLARE
                    submission_response_id uuid;
                    execution_record record;
                BEGIN
                    SELECT "ResponseId" INTO submission_response_id
                    FROM "ResponseFormulaSubmissionSets"
                    WHERE "Id" = NEW."ResponseFormulaSubmissionSetId";

                    SELECT execution."Status", execution."Authority", execution."Trigger",
                           snapshot."ResponseId", snapshot."PlacementKey"
                    INTO execution_record
                    FROM "FormulaExecutions" execution
                    JOIN "ResponseFormulaSnapshots" snapshot
                      ON snapshot."Id" = execution."ResponseFormulaSnapshotId"
                    WHERE execution."Id" = NEW."FormulaExecutionId";

                    IF submission_response_id IS NULL OR NOT FOUND
                       OR execution_record."ResponseId" <> submission_response_id
                       OR execution_record."PlacementKey" <> NEW."PlacementKey"
                       OR execution_record."Status" <> 0
                       OR execution_record."Authority" <> 1
                       OR execution_record."Trigger" <> 1 THEN
                        RAISE EXCEPTION 'Submission membership requires a valid authoritative final-submission execution for the same placement'
                            USING ERRCODE = '55000';
                    END IF;
                    RETURN NEW;
                END;
                $formula$;

                CREATE TRIGGER "TR_ResponseFormulaSubmissionExecutions_Integrity"
                    BEFORE INSERT ON "ResponseFormulaSubmissionExecutions"
                    FOR EACH ROW EXECUTE FUNCTION oryx_formula_v1_guard_submission_member();

                CREATE OR REPLACE FUNCTION oryx_formula_v1_guard_approval_link()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $formula$
                DECLARE
                    submission_response_id uuid;
                BEGIN
                    IF TG_OP = 'UPDATE' AND OLD."FormulaSubmissionSetId" IS NOT NULL
                       AND OLD."FormulaSubmissionSetId" IS DISTINCT FROM NEW."FormulaSubmissionSetId" THEN
                        RAISE EXCEPTION 'An approval formula submission set link cannot be replaced or cleared'
                            USING ERRCODE = '55000';
                    END IF;
                    IF NEW."FormulaSubmissionSetId" IS NULL THEN
                        RETURN NEW;
                    END IF;

                    SELECT "ResponseId" INTO submission_response_id
                    FROM "ResponseFormulaSubmissionSets"
                    WHERE "Id" = NEW."FormulaSubmissionSetId";
                    IF NOT FOUND OR submission_response_id <> NEW."ResponseId" THEN
                        RAISE EXCEPTION 'Approval and formula submission set must belong to the same response'
                            USING ERRCODE = '55000';
                    END IF;
                    RETURN NEW;
                END;
                $formula$;

                CREATE TRIGGER "TR_ResponseApprovals_FormulaSubmissionSet"
                    BEFORE INSERT OR UPDATE OF "FormulaSubmissionSetId", "ResponseId"
                    ON "ResponseApprovals"
                    FOR EACH ROW EXECUTE FUNCTION oryx_formula_v1_guard_approval_link();
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS "TR_ResponseApprovals_FormulaSubmissionSet" ON "ResponseApprovals";
                DROP FUNCTION IF EXISTS oryx_formula_v1_guard_approval_link();
                DROP FUNCTION IF EXISTS oryx_formula_v1_guard_submission_member() CASCADE;
                DROP FUNCTION IF EXISTS oryx_formula_v1_guard_execution() CASCADE;
                DROP FUNCTION IF EXISTS oryx_formula_v1_guard_snapshot() CASCADE;
                DROP FUNCTION IF EXISTS oryx_formula_v1_guard_formula_configuration() CASCADE;
                DROP FUNCTION IF EXISTS oryx_formula_v1_guard_form_field_revision() CASCADE;
                DROP FUNCTION IF EXISTS oryx_formula_v1_guard_form_revision() CASCADE;
                DROP FUNCTION IF EXISTS oryx_formula_v1_require_revision_audit() CASCADE;
                DROP FUNCTION IF EXISTS oryx_formula_v1_guard_revision() CASCADE;
                DROP FUNCTION IF EXISTS oryx_formula_v1_reject_mutation() CASCADE;

                ALTER TABLE "ResponseFormulaSubmissionSets"
                    DROP CONSTRAINT IF EXISTS "CK_ResponseFormulaSubmissionSet_Hashes";
                ALTER TABLE "FormulaMigrationRuns"
                    DROP CONSTRAINT IF EXISTS "CK_FormulaMigrationRun_CountBalance",
                    DROP CONSTRAINT IF EXISTS "CK_FormulaMigrationRun_CorpusChecksum";
                ALTER TABLE "FormulaExecutions"
                    DROP CONSTRAINT IF EXISTS "CK_FormulaExecution_EngineBuildHash";
                ALTER TABLE "FormFieldRevisions"
                    DROP CONSTRAINT IF EXISTS "CK_FormFieldRevision_FieldHash";
                ALTER TABLE "FormulaRevisionAudits"
                    DROP CONSTRAINT IF EXISTS "CK_FormulaRevisionAudit_Reason",
                    DROP CONSTRAINT IF EXISTS "CK_FormulaRevisionAudit_CorrelationId",
                    DROP CONSTRAINT IF EXISTS "CK_FormulaRevisionAudit_Action",
                    DROP CONSTRAINT IF EXISTS "CK_FormulaRevisionAudit_Statuses",
                    DROP CONSTRAINT IF EXISTS "CK_FormulaRevisionAudit_DefinitionHash";
                """
            );
        }
    }
}
