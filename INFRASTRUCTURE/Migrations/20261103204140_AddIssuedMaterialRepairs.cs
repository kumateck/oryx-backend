using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddIssuedMaterialRepairs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IssuedMaterialRepairs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionScheduleProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequisitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    AppliedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PreviewToken = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssuedMaterialRepairs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IssuedMaterialRepairs_ProductionScheduleProducts_Production~",
                        column: x => x.ProductionScheduleProductId,
                        principalTable: "ProductionScheduleProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IssuedMaterialRepairs_Requisitions_RequisitionId",
                        column: x => x.RequisitionId,
                        principalTable: "Requisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IssuedMaterialRepairs_users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IssuedMaterialRepairAllocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IssuedMaterialRepairId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialBatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShelfId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReservationId = table.Column<Guid>(type: "uuid", nullable: false),
                    MovementId = table.Column<Guid>(type: "uuid", nullable: false),
                    BinCardId = table.Column<Guid>(type: "uuid", nullable: false),
                    Adjustment = table.Column<decimal>(type: "numeric", nullable: false),
                    ShelfBefore = table.Column<decimal>(type: "numeric", nullable: false),
                    ShelfAfter = table.Column<decimal>(type: "numeric", nullable: false),
                    AssignedBefore = table.Column<decimal>(type: "numeric", nullable: false),
                    AssignedAfter = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssuedMaterialRepairAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IssuedMaterialRepairAllocations_IssuedMaterialRepairs_Issue~",
                        column: x => x.IssuedMaterialRepairId,
                        principalTable: "IssuedMaterialRepairs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IssuedMaterialRepairLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IssuedMaterialRepairId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequisitionItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    UoMId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialCode = table.Column<string>(type: "text", nullable: true),
                    MaterialName = table.Column<string>(type: "text", nullable: true),
                    UnitSymbol = table.Column<string>(type: "text", nullable: true),
                    ApprovedQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    BeforeQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    TargetQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    Adjustment = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssuedMaterialRepairLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IssuedMaterialRepairLines_IssuedMaterialRepairs_IssuedMater~",
                        column: x => x.IssuedMaterialRepairId,
                        principalTable: "IssuedMaterialRepairs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IssuedMaterialRepairAllocations_IssuedMaterialRepairId",
                table: "IssuedMaterialRepairAllocations",
                column: "IssuedMaterialRepairId");

            migrationBuilder.CreateIndex(
                name: "IX_IssuedMaterialRepairLines_IssuedMaterialRepairId_MaterialId",
                table: "IssuedMaterialRepairLines",
                columns: new[] { "IssuedMaterialRepairId", "MaterialId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IssuedMaterialRepairs_ActorId",
                table: "IssuedMaterialRepairs",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_IssuedMaterialRepairs_ProductionScheduleProductId_Kind_Appl~",
                table: "IssuedMaterialRepairs",
                columns: new[] { "ProductionScheduleProductId", "Kind", "AppliedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_IssuedMaterialRepairs_RequisitionId",
                table: "IssuedMaterialRepairs",
                column: "RequisitionId");

            migrationBuilder.Sql("""
                CREATE FUNCTION public.reject_issued_material_repair_change() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN
                  RAISE EXCEPTION 'Issued material repair evidence is append-only';
                END $$;
                CREATE TRIGGER issued_repair_append_only BEFORE UPDATE OR DELETE OR TRUNCATE ON "IssuedMaterialRepairs"
                  FOR EACH STATEMENT EXECUTE FUNCTION public.reject_issued_material_repair_change();
                CREATE TRIGGER issued_repair_line_append_only BEFORE UPDATE OR DELETE OR TRUNCATE ON "IssuedMaterialRepairLines"
                  FOR EACH STATEMENT EXECUTE FUNCTION public.reject_issued_material_repair_change();
                CREATE TRIGGER issued_repair_allocation_append_only BEFORE UPDATE OR DELETE OR TRUNCATE ON "IssuedMaterialRepairAllocations"
                  FOR EACH STATEMENT EXECUTE FUNCTION public.reject_issued_material_repair_change();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IssuedMaterialRepairAllocations");

            migrationBuilder.DropTable(
                name: "IssuedMaterialRepairLines");

            migrationBuilder.DropTable(
                name: "IssuedMaterialRepairs");
            migrationBuilder.Sql("DROP FUNCTION public.reject_issued_material_repair_change()");
        }
    }
}
