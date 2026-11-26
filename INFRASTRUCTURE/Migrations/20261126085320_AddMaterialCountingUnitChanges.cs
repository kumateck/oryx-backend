using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialCountingUnitChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MaterialCountingUnitChanges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialDepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FromUnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToUnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    DensityGramsPerMilliliter = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    PreviewHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    PreviewJson = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialCountingUnitChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialCountingUnitChanges_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaterialCountingUnitChanges_MaterialDepartments_MaterialDep~",
                        column: x => x.MaterialDepartmentId,
                        principalTable: "MaterialDepartments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaterialCountingUnitChanges_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaterialCountingUnitChanges_UnitOfMeasures_FromUnitId",
                        column: x => x.FromUnitId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaterialCountingUnitChanges_UnitOfMeasures_ToUnitId",
                        column: x => x.ToUnitId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaterialCountingUnitChanges_users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MaterialCountingUnitChanges_ActorId",
                table: "MaterialCountingUnitChanges",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialCountingUnitChanges_DepartmentId",
                table: "MaterialCountingUnitChanges",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialCountingUnitChanges_FromUnitId",
                table: "MaterialCountingUnitChanges",
                column: "FromUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialCountingUnitChanges_MaterialDepartmentId",
                table: "MaterialCountingUnitChanges",
                column: "MaterialDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialCountingUnitChanges_MaterialId_DepartmentId_Recorde~",
                table: "MaterialCountingUnitChanges",
                columns: new[] { "MaterialId", "DepartmentId", "RecordedAt" });

            migrationBuilder.Sql("""
                CREATE FUNCTION public.oryx_material_counting_unit_immutable() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN RAISE EXCEPTION 'Material counting-unit changes are append-only'; END; $$;
                CREATE TRIGGER material_counting_unit_immutable BEFORE UPDATE OR DELETE ON "MaterialCountingUnitChanges"
                    FOR EACH ROW EXECUTE FUNCTION public.oryx_material_counting_unit_immutable();
                CREATE TRIGGER material_counting_unit_no_truncate BEFORE TRUNCATE ON "MaterialCountingUnitChanges"
                    FOR EACH STATEMENT EXECUTE FUNCTION public.oryx_material_counting_unit_immutable();
                """);

            migrationBuilder.CreateIndex(
                name: "IX_MaterialCountingUnitChanges_ToUnitId",
                table: "MaterialCountingUnitChanges",
                column: "ToUnitId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MaterialCountingUnitChanges");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS public.oryx_material_counting_unit_immutable();");
        }
    }
}
