using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduleProductRepairs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductionScheduleRepairs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScheduleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    AppliedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    PreviewHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    BeforeJson = table.Column<string>(type: "text", nullable: true),
                    AfterJson = table.Column<string>(type: "text", nullable: true),
                    ResultJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionScheduleRepairs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionScheduleRepairs_ProductionSchedules_ScheduleId",
                        column: x => x.ScheduleId,
                        principalTable: "ProductionSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionScheduleRepairs_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionScheduleRepairs_users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionScheduleRepairs_ActorId",
                table: "ProductionScheduleRepairs",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionScheduleRepairs_ProductId",
                table: "ProductionScheduleRepairs",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionScheduleRepairs_ScheduleId_ProductId_AppliedAt",
                table: "ProductionScheduleRepairs",
                columns: new[] { "ScheduleId", "ProductId", "AppliedAt" });
            migrationBuilder.Sql("""
                CREATE FUNCTION prevent_schedule_repair_evidence_change() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN
                    RAISE EXCEPTION 'Production schedule repair evidence is append-only' USING ERRCODE = '23514';
                END;
                $$;
                CREATE TRIGGER schedule_repair_evidence_append_only
                BEFORE UPDATE OR DELETE ON "ProductionScheduleRepairs"
                FOR EACH ROW EXECUTE FUNCTION prevent_schedule_repair_evidence_change();
                CREATE TRIGGER schedule_repair_evidence_no_truncate
                BEFORE TRUNCATE ON "ProductionScheduleRepairs"
                FOR EACH STATEMENT EXECUTE FUNCTION prevent_schedule_repair_evidence_change();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductionScheduleRepairs");
            migrationBuilder.Sql("DROP FUNCTION prevent_schedule_repair_evidence_change()");
        }
    }
}
