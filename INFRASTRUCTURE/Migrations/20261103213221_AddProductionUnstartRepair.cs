using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionUnstartRepair : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(name: "ProductionUnstartRepairId", table: "MaterialBatchReservedQuantities", type: "uuid", nullable: true);
            migrationBuilder.DropIndex(
                name: "IX_ProductionActivities_ProductionScheduleProductId",
                table: "ProductionActivities");

            migrationBuilder.CreateTable(
                name: "ProductionUnstartRepairs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActivityId = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    AppliedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PreviewToken = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    BeforeJson = table.Column<string>(type: "text", nullable: true),
                    AfterJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionUnstartRepairs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionUnstartRepairs_ProductionActivities_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "ProductionActivities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionUnstartRepairs_ProductionScheduleProducts_BatchId",
                        column: x => x.BatchId,
                        principalTable: "ProductionScheduleProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionUnstartRepairs_users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionActivities_ProductionScheduleProductId",
                table: "ProductionActivities",
                column: "ProductionScheduleProductId",
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionUnstartRepairs_ActivityId",
                table: "ProductionUnstartRepairs",
                column: "ActivityId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionUnstartRepairs_ActorId",
                table: "ProductionUnstartRepairs",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionUnstartRepairs_BatchId",
                table: "ProductionUnstartRepairs",
                column: "BatchId");

            migrationBuilder.Sql("""
                CREATE FUNCTION public.guard_unstarted_production_reference() RETURNS trigger LANGUAGE plpgsql AS $$
                DECLARE reference_id uuid; active boolean;
                BEGIN
                  IF to_jsonb(NEW)->>'DeletedAt' IS NOT NULL THEN RETURN NEW; END IF;
                  reference_id := (to_jsonb(NEW)->>TG_ARGV[2])::uuid;
                  IF reference_id IS NULL THEN RETURN NEW; END IF;
                  EXECUTE format('SELECT "DeletedAt" IS NULL FROM %I.%I WHERE "Id"=$1 FOR SHARE', TG_ARGV[0], TG_ARGV[1])
                    INTO active USING reference_id;
                  IF active IS FALSE THEN
                    RAISE EXCEPTION 'Production run or draft document was unstarted; reload before continuing' USING ERRCODE='23514';
                  END IF;
                  RETURN NEW;
                END $$;
                CREATE FUNCTION public.guard_unstarted_production_history() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN
                  IF OLD."DeletedAt" IS NOT NULL THEN
                    RAISE EXCEPTION 'Archived production history cannot be changed' USING ERRCODE='23514';
                  END IF;
                  RETURN NEW;
                END $$;
                CREATE FUNCTION public.reject_production_unstart_audit_change() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN RAISE EXCEPTION 'Production unstart evidence is append-only'; END $$;
                CREATE FUNCTION public.guard_unstarted_batch_reservation() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN
                  IF TG_OP='UPDATE' AND OLD."ProductionUnstartRepairId" IS NOT NULL THEN
                    RAISE EXCEPTION 'Released reservation history cannot be changed' USING ERRCODE='23514';
                  END IF;
                  IF NEW."DeletedAt" IS NULL
                    AND EXISTS (SELECT 1 FROM "ProductionUnstartRepairs" WHERE "BatchId"=NEW."ProductionScheduleProductId")
                    AND NOT EXISTS (SELECT 1 FROM "ProductionActivities" WHERE "ProductionScheduleProductId"=NEW."ProductionScheduleProductId" AND "DeletedAt" IS NULL)
                  THEN RAISE EXCEPTION 'Production was unstarted; start a new run before reserving stock' USING ERRCODE='23514'; END IF;
                  RETURN NEW;
                END $$;
                CREATE TRIGGER unstarted_batch_reservation_guard BEFORE INSERT OR UPDATE ON "MaterialBatchReservedQuantities"
                  FOR EACH ROW EXECUTE FUNCTION public.guard_unstarted_batch_reservation();
                CREATE TRIGGER production_unstart_audit_append_only BEFORE UPDATE OR DELETE OR TRUNCATE ON "ProductionUnstartRepairs"
                  FOR EACH STATEMENT EXECUTE FUNCTION public.reject_production_unstart_audit_change();
                DO $guards$
                DECLARE relation record; table_name text;
                BEGIN
                  FOR relation IN
                    SELECT child_ns.nspname AS child_schema, child.relname AS child_table, parent_ns.nspname AS parent_schema,
                      parent.relname AS parent_table, attribute.attname AS column_name, constraint_row.oid AS constraint_id
                    FROM pg_constraint constraint_row
                    JOIN pg_class child ON child.oid=constraint_row.conrelid JOIN pg_namespace child_ns ON child_ns.oid=child.relnamespace
                    JOIN pg_class parent ON parent.oid=constraint_row.confrelid JOIN pg_namespace parent_ns ON parent_ns.oid=parent.relnamespace
                    JOIN pg_attribute attribute ON attribute.attrelid=child.oid AND attribute.attnum=constraint_row.conkey[1]
                    WHERE constraint_row.contype='f' AND array_length(constraint_row.conkey,1)=1 AND parent_ns.nspname='public'
                      AND parent.relname IN ('ProductionActivities','ProductionActivitySteps','BatchManufacturingRecords','BatchPackagingRecords')
                      AND child.relname NOT IN ('ProductionActivityLogs','ProductionUnstartRepairs')
                  LOOP
                    EXECUTE format('CREATE TRIGGER %I BEFORE INSERT OR UPDATE ON %I.%I FOR EACH ROW EXECUTE FUNCTION public.guard_unstarted_production_reference(%L,%L,%L)',
                      'unstart_reference_'||relation.constraint_id,relation.child_schema,relation.child_table,relation.parent_schema,relation.parent_table,relation.column_name);
                  END LOOP;
                  FOREACH table_name IN ARRAY ARRAY['ProductionActivities','ProductionActivitySteps','BatchManufacturingRecords','BatchPackagingRecords'] LOOP
                    EXECUTE format('CREATE TRIGGER unstarted_history_guard BEFORE UPDATE ON public.%I FOR EACH ROW EXECUTE FUNCTION public.guard_unstarted_production_history()',table_name);
                  END LOOP;
                END $guards$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ProductionUnstartRepairId", table: "MaterialBatchReservedQuantities");
            migrationBuilder.Sql("""
                DROP FUNCTION public.guard_unstarted_production_reference() CASCADE;
                DROP FUNCTION public.guard_unstarted_production_history() CASCADE;
                DROP FUNCTION public.reject_production_unstart_audit_change() CASCADE;
                DROP FUNCTION public.guard_unstarted_batch_reservation() CASCADE;
                """);
            migrationBuilder.DropTable(
                name: "ProductionUnstartRepairs");

            migrationBuilder.DropIndex(
                name: "IX_ProductionActivities_ProductionScheduleProductId",
                table: "ProductionActivities");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionActivities_ProductionScheduleProductId",
                table: "ProductionActivities",
                column: "ProductionScheduleProductId",
                unique: true);
        }
    }
}
