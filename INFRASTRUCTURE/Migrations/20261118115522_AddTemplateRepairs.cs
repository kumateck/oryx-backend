using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddTemplateRepairs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TemplateRepairs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FormId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    PreviewToken = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RequestHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EvidenceJson = table.Column<string>(type: "jsonb", nullable: false),
                    AppliedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateRepairs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplateRepairs_Forms_FormId",
                        column: x => x.FormId,
                        principalTable: "Forms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateRepairs_users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TemplateRepairAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RepairId = table.Column<Guid>(type: "uuid", nullable: false),
                    FormAssigneeId = table.Column<Guid>(type: "uuid", nullable: false),
                    FormFieldAssigneeId = table.Column<Guid>(type: "uuid", nullable: false),
                    FormFieldId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssigneeId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateRepairAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplateRepairAssignments_FormAssignees_FormAssigneeId",
                        column: x => x.FormAssigneeId,
                        principalTable: "FormAssignees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateRepairAssignments_FormFields_FormFieldId",
                        column: x => x.FormFieldId,
                        principalTable: "FormFields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateRepairAssignments_TemplateRepairs_RepairId",
                        column: x => x.RepairId,
                        principalTable: "TemplateRepairs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateRepairAssignments_users_AssigneeId",
                        column: x => x.AssigneeId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TemplateRepairAssignments_AssigneeId",
                table: "TemplateRepairAssignments",
                column: "AssigneeId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateRepairAssignments_FormAssigneeId_FormFieldId",
                table: "TemplateRepairAssignments",
                columns: new[] { "FormAssigneeId", "FormFieldId" });

            migrationBuilder.CreateIndex(
                name: "IX_TemplateRepairAssignments_FormFieldId",
                table: "TemplateRepairAssignments",
                column: "FormFieldId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateRepairAssignments_RepairId",
                table: "TemplateRepairAssignments",
                column: "RepairId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateRepairs_ActorId",
                table: "TemplateRepairs",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateRepairs_FormId",
                table: "TemplateRepairs",
                column: "FormId");
            // Template repair history is append-only, like formula repair history.
            migrationBuilder.Sql("""
                CREATE FUNCTION public.oryx_template_repair_immutable() RETURNS trigger LANGUAGE plpgsql AS $guard$
                BEGIN RAISE EXCEPTION 'Template repair history is append-only'; END; $guard$;
                CREATE TRIGGER template_repair_immutable BEFORE UPDATE OR DELETE ON public."TemplateRepairs"
                  FOR EACH ROW EXECUTE FUNCTION public.oryx_template_repair_immutable();
                CREATE TRIGGER template_repair_assignment_immutable BEFORE UPDATE OR DELETE ON public."TemplateRepairAssignments"
                  FOR EACH ROW EXECUTE FUNCTION public.oryx_template_repair_immutable();
                CREATE TRIGGER template_repair_no_truncate BEFORE TRUNCATE ON public."TemplateRepairs"
                  FOR EACH STATEMENT EXECUTE FUNCTION public.oryx_template_repair_immutable();
                CREATE TRIGGER template_repair_assignment_no_truncate BEFORE TRUNCATE ON public."TemplateRepairAssignments"
                  FOR EACH STATEMENT EXECUTE FUNCTION public.oryx_template_repair_immutable();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS template_repair_assignment_no_truncate ON public."TemplateRepairAssignments";
                DROP TRIGGER IF EXISTS template_repair_no_truncate ON public."TemplateRepairs";
                DROP TRIGGER IF EXISTS template_repair_assignment_immutable ON public."TemplateRepairAssignments";
                DROP TRIGGER IF EXISTS template_repair_immutable ON public."TemplateRepairs";
                DROP FUNCTION IF EXISTS public.oryx_template_repair_immutable();
                """);
            migrationBuilder.DropTable(
                name: "TemplateRepairAssignments");

            migrationBuilder.DropTable(
                name: "TemplateRepairs");
        }
    }
}
