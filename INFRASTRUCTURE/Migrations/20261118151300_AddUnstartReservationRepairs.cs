using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddUnstartReservationRepairs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UnstartReservationRepairs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    AppliedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PreviewToken = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    BeforeJson = table.Column<string>(type: "text", nullable: true),
                    ReservationIdsJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnstartReservationRepairs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UnstartReservationRepairs_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UnstartReservationRepairs_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UnstartReservationRepairs_users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UnstartReservationRepairs_ActorId",
                table: "UnstartReservationRepairs",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_UnstartReservationRepairs_DepartmentId",
                table: "UnstartReservationRepairs",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_UnstartReservationRepairs_MaterialId",
                table: "UnstartReservationRepairs",
                column: "MaterialId");
            migrationBuilder.Sql("""
                CREATE FUNCTION public.oryx_unstart_reservations_immutable() RETURNS trigger LANGUAGE plpgsql AS $guard$
                BEGIN RAISE EXCEPTION 'Unstart reservation repair history is append-only'; END; $guard$;
                CREATE TRIGGER unstart_reservations_immutable BEFORE UPDATE OR DELETE ON public."UnstartReservationRepairs"
                  FOR EACH ROW EXECUTE FUNCTION public.oryx_unstart_reservations_immutable();
                CREATE TRIGGER unstart_reservations_no_truncate BEFORE TRUNCATE ON public."UnstartReservationRepairs"
                  FOR EACH STATEMENT EXECUTE FUNCTION public.oryx_unstart_reservations_immutable();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS unstart_reservations_immutable ON public."UnstartReservationRepairs";
                DROP TRIGGER IF EXISTS unstart_reservations_no_truncate ON public."UnstartReservationRepairs";
                DROP FUNCTION IF EXISTS public.oryx_unstart_reservations_immutable();
                """);
            migrationBuilder.DropTable(
                name: "UnstartReservationRepairs");
        }
    }
}
