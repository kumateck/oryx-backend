using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddLegacyFormulaRepairs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QuestionFormulaRepairs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedQuestion = table.Column<bool>(type: "boolean", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    PreviewToken = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RequestHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DefinitionBefore = table.Column<string>(type: "text", nullable: false),
                    DefinitionAfter = table.Column<string>(type: "text", nullable: false),
                    EvidenceJson = table.Column<string>(type: "jsonb", nullable: false),
                    AppliedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionFormulaRepairs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestionFormulaRepairs_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuestionFormulaRepairs_users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LegacyFormulaConfigurationPins",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ResponseId = table.Column<Guid>(type: "uuid", nullable: false),
                    FormFieldId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    RepairId = table.Column<Guid>(type: "uuid", nullable: false),
                    Definition = table.Column<string>(type: "text", nullable: false),
                    DefinitionHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RequiresRecalculation = table.Column<bool>(type: "boolean", nullable: false),
                    CapturedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegacyFormulaConfigurationPins", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LegacyFormulaConfigurationPins_FormFields_FormFieldId",
                        column: x => x.FormFieldId,
                        principalTable: "FormFields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LegacyFormulaConfigurationPins_QuestionFormulaRepairs_Repai~",
                        column: x => x.RepairId,
                        principalTable: "QuestionFormulaRepairs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LegacyFormulaConfigurationPins_Responses_ResponseId",
                        column: x => x.ResponseId,
                        principalTable: "Responses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LegacyFormulaRepairEvaluations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PinId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    InputHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ResultsJson = table.Column<string>(type: "jsonb", nullable: false),
                    EngineBuildHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EvidenceJson = table.Column<string>(type: "jsonb", nullable: false),
                    EvaluatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegacyFormulaRepairEvaluations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LegacyFormulaRepairEvaluations_LegacyFormulaConfigurationPi~",
                        column: x => x.PinId,
                        principalTable: "LegacyFormulaConfigurationPins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LegacyFormulaRepairEvaluations_users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LegacyFormulaConfigurationPins_FormFieldId",
                table: "LegacyFormulaConfigurationPins",
                column: "FormFieldId");

            migrationBuilder.CreateIndex(
                name: "IX_LegacyFormulaConfigurationPins_RepairId",
                table: "LegacyFormulaConfigurationPins",
                column: "RepairId");

            migrationBuilder.CreateIndex(
                name: "IX_LegacyFormulaConfigurationPins_ResponseId_FormFieldId_Seque~",
                table: "LegacyFormulaConfigurationPins",
                columns: new[] { "ResponseId", "FormFieldId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LegacyFormulaRepairEvaluations_ActorId",
                table: "LegacyFormulaRepairEvaluations",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_LegacyFormulaRepairEvaluations_PinId_InputHash",
                table: "LegacyFormulaRepairEvaluations",
                columns: new[] { "PinId", "InputHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuestionFormulaRepairs_ActorId",
                table: "QuestionFormulaRepairs",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionFormulaRepairs_QuestionId",
                table: "QuestionFormulaRepairs",
                column: "QuestionId");
            migrationBuilder.Sql("""
                CREATE FUNCTION public.oryx_formula_repair_immutable() RETURNS trigger LANGUAGE plpgsql AS $guard$
                BEGIN RAISE EXCEPTION 'Formula repair history is append-only'; END; $guard$;
                CREATE TRIGGER formula_repair_immutable BEFORE UPDATE OR DELETE ON public."QuestionFormulaRepairs"
                  FOR EACH ROW EXECUTE FUNCTION public.oryx_formula_repair_immutable();
                CREATE TRIGGER formula_pin_immutable BEFORE UPDATE OR DELETE ON public."LegacyFormulaConfigurationPins"
                  FOR EACH ROW EXECUTE FUNCTION public.oryx_formula_repair_immutable();
                CREATE TRIGGER formula_repair_evaluation_immutable BEFORE UPDATE OR DELETE ON public."LegacyFormulaRepairEvaluations"
                  FOR EACH ROW EXECUTE FUNCTION public.oryx_formula_repair_immutable();
                CREATE TRIGGER formula_repair_no_truncate BEFORE TRUNCATE ON public."QuestionFormulaRepairs"
                  FOR EACH STATEMENT EXECUTE FUNCTION public.oryx_formula_repair_immutable();
                CREATE TRIGGER formula_pin_no_truncate BEFORE TRUNCATE ON public."LegacyFormulaConfigurationPins"
                  FOR EACH STATEMENT EXECUTE FUNCTION public.oryx_formula_repair_immutable();
                CREATE TRIGGER formula_repair_evaluation_no_truncate BEFORE TRUNCATE ON public."LegacyFormulaRepairEvaluations"
                  FOR EACH STATEMENT EXECUTE FUNCTION public.oryx_formula_repair_immutable();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS formula_repair_evaluation_no_truncate ON public."LegacyFormulaRepairEvaluations";
                DROP TRIGGER IF EXISTS formula_pin_no_truncate ON public."LegacyFormulaConfigurationPins";
                DROP TRIGGER IF EXISTS formula_repair_no_truncate ON public."QuestionFormulaRepairs";
                DROP TRIGGER IF EXISTS formula_repair_evaluation_immutable ON public."LegacyFormulaRepairEvaluations";
                DROP TRIGGER IF EXISTS formula_pin_immutable ON public."LegacyFormulaConfigurationPins";
                DROP TRIGGER IF EXISTS formula_repair_immutable ON public."QuestionFormulaRepairs";
                DROP FUNCTION IF EXISTS public.oryx_formula_repair_immutable();
                """);
            migrationBuilder.DropTable(
                name: "LegacyFormulaRepairEvaluations");

            migrationBuilder.DropTable(
                name: "LegacyFormulaConfigurationPins");

            migrationBuilder.DropTable(
                name: "QuestionFormulaRepairs");
        }
    }
}
