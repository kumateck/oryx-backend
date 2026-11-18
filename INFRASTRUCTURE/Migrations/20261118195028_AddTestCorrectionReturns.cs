using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddTestCorrectionReturns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TestCorrectionReturns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceResponseId = table.Column<Guid>(type: "uuid", nullable: false),
                    CorrectionResponseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReturnedById = table.Column<Guid>(type: "uuid", nullable: false),
                    ReturnedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    PreviousState = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestCorrectionReturns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestCorrectionReturns_Responses_CorrectionResponseId",
                        column: x => x.CorrectionResponseId,
                        principalTable: "Responses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TestCorrectionReturns_Responses_SourceResponseId",
                        column: x => x.SourceResponseId,
                        principalTable: "Responses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TestCorrectionReturns_users_ReturnedById",
                        column: x => x.ReturnedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TestCorrectionReturns_CorrectionResponseId",
                table: "TestCorrectionReturns",
                column: "CorrectionResponseId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TestCorrectionReturns_ReturnedById",
                table: "TestCorrectionReturns",
                column: "ReturnedById");

            migrationBuilder.CreateIndex(
                name: "IX_TestCorrectionReturns_SourceResponseId",
                table: "TestCorrectionReturns",
                column: "SourceResponseId",
                unique: true);
            migrationBuilder.Sql("""
                CREATE FUNCTION public.oryx_test_correction_immutable() RETURNS trigger LANGUAGE plpgsql AS $guard$
                BEGIN RAISE EXCEPTION 'Test correction history is append-only'; END; $guard$;
                CREATE TRIGGER test_correction_immutable BEFORE UPDATE OR DELETE ON public."TestCorrectionReturns"
                  FOR EACH ROW EXECUTE FUNCTION public.oryx_test_correction_immutable();
                CREATE TRIGGER test_correction_no_truncate BEFORE TRUNCATE ON public."TestCorrectionReturns"
                  FOR EACH STATEMENT EXECUTE FUNCTION public.oryx_test_correction_immutable();
                CREATE FUNCTION public.oryx_returned_test_immutable() RETURNS trigger LANGUAGE plpgsql AS $guard$
                BEGIN
                  IF TG_TABLE_NAME = 'Responses' THEN
                    IF EXISTS (SELECT 1 FROM public."TestCorrectionReturns" h WHERE h."SourceResponseId" = OLD."Id") THEN
                      RAISE EXCEPTION 'Returned test evidence is immutable';
                    END IF;
                  ELSE
                    IF EXISTS (SELECT 1 FROM public."TestCorrectionReturns" h WHERE h."SourceResponseId" = (CASE WHEN TG_OP = 'INSERT' THEN NEW."ResponseId" ELSE OLD."ResponseId" END)) THEN
                      RAISE EXCEPTION 'Returned test evidence is immutable';
                    END IF;
                  END IF;
                  IF TG_OP = 'DELETE' THEN RETURN OLD; END IF;
                  RETURN NEW;
                END; $guard$;
                CREATE TRIGGER returned_response_immutable BEFORE UPDATE OR DELETE ON public."Responses"
                  FOR EACH ROW EXECUTE FUNCTION public.oryx_returned_test_immutable();
                CREATE TRIGGER returned_answers_immutable BEFORE INSERT OR UPDATE OR DELETE ON public."FormResponses"
                  FOR EACH ROW EXECUTE FUNCTION public.oryx_returned_test_immutable();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS returned_response_immutable ON public."Responses";
                DROP TRIGGER IF EXISTS returned_answers_immutable ON public."FormResponses";
                DROP FUNCTION IF EXISTS public.oryx_returned_test_immutable();
                DROP TRIGGER IF EXISTS test_correction_no_truncate ON public."TestCorrectionReturns";
                DROP TRIGGER IF EXISTS test_correction_immutable ON public."TestCorrectionReturns";
                DROP FUNCTION IF EXISTS public.oryx_test_correction_immutable();
                """);
            migrationBuilder.DropTable(
                name: "TestCorrectionReturns");
        }
    }
}
