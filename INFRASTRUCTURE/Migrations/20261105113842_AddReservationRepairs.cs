using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddReservationRepairs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReservationRepairs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    MigrationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    AppliedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    PreviewToken = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    BeforeJson = table.Column<string>(type: "text", nullable: false),
                    AfterJson = table.Column<string>(type: "text", nullable: false),
                    RowsJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservationRepairs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReservationRepairs_ProductionScheduleProducts_BatchId",
                        column: x => x.BatchId,
                        principalTable: "ProductionScheduleProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReservationRepairs_StockMigrations_MigrationId",
                        column: x => x.MigrationId,
                        principalTable: "StockMigrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReservationRepairs_users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReservationRepairs_ActorId",
                table: "ReservationRepairs",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_ReservationRepairs_BatchId",
                table: "ReservationRepairs",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_ReservationRepairs_MigrationId",
                table: "ReservationRepairs",
                column: "MigrationId");

            migrationBuilder.CreateIndex(
                name: "IX_ReservationRepairs_PreviewToken",
                table: "ReservationRepairs",
                column: "PreviewToken",
                unique: true);
            migrationBuilder.Sql("""
                CREATE FUNCTION public.reject_reservation_repair_audit_change() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN RAISE EXCEPTION 'Reservation repair evidence is append-only'; END $$;
                CREATE TRIGGER reservation_repair_audit_append_only BEFORE UPDATE OR DELETE OR TRUNCATE
                  ON public."ReservationRepairs" FOR EACH STATEMENT
                  EXECUTE FUNCTION public.reject_reservation_repair_audit_change();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP FUNCTION public.reject_reservation_repair_audit_change() CASCADE");
            migrationBuilder.DropTable(
                name: "ReservationRepairs");
        }
    }
}
