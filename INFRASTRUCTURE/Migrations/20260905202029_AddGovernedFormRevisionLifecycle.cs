using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddGovernedFormRevisionLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RetiredAt",
                table: "FormRevisions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                table: "FormRevisions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReviewedById",
                table: "FormRevisions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FormRevisionAudits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FormRevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PriorStatus = table.Column<int>(type: "integer", nullable: true),
                    NewStatus = table.Column<int>(type: "integer", nullable: false),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Reason = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormRevisionAudits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormRevisionAudits_FormRevisions_FormRevisionId",
                        column: x => x.FormRevisionId,
                        principalTable: "FormRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FormRevisionAudits_users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FormRevisions_ReviewedById",
                table: "FormRevisions",
                column: "ReviewedById");

            migrationBuilder.CreateIndex(
                name: "IX_FormRevisionAudits_ActorId",
                table: "FormRevisionAudits",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_FormRevisionAudits_FormRevisionId_OccurredAt",
                table: "FormRevisionAudits",
                columns: new[] { "FormRevisionId", "OccurredAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_FormRevisions_users_ReviewedById",
                table: "FormRevisions",
                column: "ReviewedById",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                CREATE TRIGGER "TR_FormRevisionAudits_AppendOnly"
                    BEFORE UPDATE OR DELETE ON "FormRevisionAudits"
                    FOR EACH ROW EXECUTE FUNCTION oryx_formula_v1_reject_mutation();

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
                    IF OLD."ReviewedById" IS NOT NULL AND (
                        OLD."ReviewedById" IS DISTINCT FROM NEW."ReviewedById"
                        OR OLD."ReviewedAt" IS DISTINCT FROM NEW."ReviewedAt"
                    ) THEN
                        RAISE EXCEPTION 'Form revision review evidence is immutable'
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
                        NEW."ReviewedById" IS NULL OR NEW."ReviewedAt" IS NULL
                        OR NEW."ApprovedById" IS NULL OR NEW."ApprovedAt" IS NULL
                        OR NEW."CreatedById" = NEW."ReviewedById"
                        OR NEW."CreatedById" = NEW."ApprovedById"
                        OR NEW."ReviewedById" = NEW."ApprovedById"
                    ) THEN
                        RAISE EXCEPTION 'Approved form revisions require segregated review evidence'
                            USING ERRCODE = '55000';
                    END IF;
                    IF NEW."Status" = 3 AND NEW."RetiredAt" IS NULL THEN
                        RAISE EXCEPTION 'A retired form revision requires a retirement time'
                            USING ERRCODE = '55000';
                    END IF;
                    RETURN NEW;
                END;
                $formula$;

                CREATE OR REPLACE FUNCTION oryx_formula_v1_require_form_revision_audit()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $formula$
                BEGIN
                    IF OLD."Status" IS DISTINCT FROM NEW."Status" AND NOT EXISTS (
                        SELECT 1 FROM "FormRevisionAudits" audit
                        WHERE audit."FormRevisionId" = NEW."Id"
                          AND audit."PriorStatus" = OLD."Status"
                          AND audit."NewStatus" = NEW."Status"
                          AND audit."ContentHash" = NEW."ContentHash"
                          AND audit."ActorId" <> '00000000-0000-0000-0000-000000000000'::uuid
                          AND audit."CorrelationId" <> '00000000-0000-0000-0000-000000000000'::uuid
                          AND btrim(audit."Reason") <> ''
                          AND audit.xmin::text::bigint = txid_current()
                    ) THEN
                        RAISE EXCEPTION 'A form revision status transition requires audit evidence'
                            USING ERRCODE = '55000';
                    END IF;
                    RETURN NEW;
                END;
                $formula$;

                CREATE CONSTRAINT TRIGGER "TR_FormRevisions_StatusAudit"
                    AFTER UPDATE ON "FormRevisions"
                    DEFERRABLE INITIALLY DEFERRED
                    FOR EACH ROW
                    WHEN (OLD."Status" IS DISTINCT FROM NEW."Status")
                    EXECUTE FUNCTION oryx_formula_v1_require_form_revision_audit();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS "TR_FormRevisions_StatusAudit" ON "FormRevisions";
                DROP FUNCTION IF EXISTS oryx_formula_v1_require_form_revision_audit();
                DROP TRIGGER IF EXISTS "TR_FormRevisionAudits_AppendOnly" ON "FormRevisionAudits";

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
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_FormRevisions_users_ReviewedById",
                table: "FormRevisions");

            migrationBuilder.DropTable(
                name: "FormRevisionAudits");

            migrationBuilder.DropIndex(
                name: "IX_FormRevisions_ReviewedById",
                table: "FormRevisions");

            migrationBuilder.DropColumn(
                name: "RetiredAt",
                table: "FormRevisions");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "FormRevisions");

            migrationBuilder.DropColumn(
                name: "ReviewedById",
                table: "FormRevisions");
        }
    }
}
