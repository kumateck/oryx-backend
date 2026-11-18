using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddUnstartLeftoverRepairs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UnstartLeftoverRepairs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    AppliedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PreviewToken = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    BeforeJson = table.Column<string>(type: "text", nullable: true),
                    RequisitionIdsJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnstartLeftoverRepairs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UnstartLeftoverRepairs_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UnstartLeftoverRepairs_users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UnstartLeftoverRepairs_ActorId",
                table: "UnstartLeftoverRepairs",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_UnstartLeftoverRepairs_DepartmentId",
                table: "UnstartLeftoverRepairs",
                column: "DepartmentId");
            migrationBuilder.Sql("""
                CREATE FUNCTION public.oryx_unstart_leftover_immutable() RETURNS trigger LANGUAGE plpgsql AS $guard$
                BEGIN RAISE EXCEPTION 'Unstart leftover repair history is append-only'; END; $guard$;
                CREATE TRIGGER unstart_leftover_immutable BEFORE UPDATE OR DELETE ON public."UnstartLeftoverRepairs"
                  FOR EACH ROW EXECUTE FUNCTION public.oryx_unstart_leftover_immutable();
                CREATE TRIGGER unstart_leftover_no_truncate BEFORE TRUNCATE ON public."UnstartLeftoverRepairs"
                  FOR EACH STATEMENT EXECUTE FUNCTION public.oryx_unstart_leftover_immutable();
                """);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS unstart_leftover_immutable ON public."UnstartLeftoverRepairs";
                DROP TRIGGER IF EXISTS unstart_leftover_no_truncate ON public."UnstartLeftoverRepairs";
                DROP FUNCTION IF EXISTS public.oryx_unstart_leftover_immutable();
                """);
            migrationBuilder.DropTable(
                name: "UnstartLeftoverRepairs");
        }
    }
}
