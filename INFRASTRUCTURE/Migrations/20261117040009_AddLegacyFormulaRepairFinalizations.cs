using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddLegacyFormulaRepairFinalizations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LegacyFormulaRepairFinalizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EvaluationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResponseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    FinalizedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegacyFormulaRepairFinalizations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LegacyFormulaRepairFinalizations_LegacyFormulaRepairEvaluat~",
                        column: x => x.EvaluationId,
                        principalTable: "LegacyFormulaRepairEvaluations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LegacyFormulaRepairFinalizations_Responses_ResponseId",
                        column: x => x.ResponseId,
                        principalTable: "Responses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LegacyFormulaRepairFinalizations_users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LegacyFormulaRepairFinalizations_ActorId",
                table: "LegacyFormulaRepairFinalizations",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_LegacyFormulaRepairFinalizations_EvaluationId",
                table: "LegacyFormulaRepairFinalizations",
                column: "EvaluationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LegacyFormulaRepairFinalizations_ResponseId",
                table: "LegacyFormulaRepairFinalizations",
                column: "ResponseId");
            migrationBuilder.Sql("""
                CREATE TRIGGER formula_repair_finalization_immutable BEFORE UPDATE OR DELETE ON public."LegacyFormulaRepairFinalizations"
                  FOR EACH ROW EXECUTE FUNCTION public.oryx_formula_repair_immutable();
                CREATE TRIGGER formula_repair_finalization_no_truncate BEFORE TRUNCATE ON public."LegacyFormulaRepairFinalizations"
                  FOR EACH STATEMENT EXECUTE FUNCTION public.oryx_formula_repair_immutable();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS formula_repair_finalization_no_truncate ON public."LegacyFormulaRepairFinalizations";
                DROP TRIGGER IF EXISTS formula_repair_finalization_immutable ON public."LegacyFormulaRepairFinalizations";
                """);
            migrationBuilder.DropTable(
                name: "LegacyFormulaRepairFinalizations");
        }
    }
}
