using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionMaterialRepairs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ContentRevision",
                table: "Requisitions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "RetiredByRepairId",
                table: "RequisitionItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ContentRevision",
                table: "RequisitionApprovals",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReleasedAt",
                table: "MaterialBatchReservedQuantities",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProductionMaterialConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionScheduleProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    StorageWarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionWarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    SourceHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    RequirementsJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionMaterialConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionMaterialConfigurations_ProductionScheduleProducts~",
                        column: x => x.ProductionScheduleProductId,
                        principalTable: "ProductionScheduleProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionMaterialConfigurations_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionMaterialConfigurations_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionMaterialConfigurations_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProductionMaterialRepairs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionScheduleProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    PreviewHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    BeforeJson = table.Column<string>(type: "text", nullable: true),
                    AfterJson = table.Column<string>(type: "text", nullable: true),
                    ResultJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionMaterialRepairs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionMaterialRepairs_ProductionScheduleProducts_Produc~",
                        column: x => x.ProductionScheduleProductId,
                        principalTable: "ProductionScheduleProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionMaterialRepairs_users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionMaterialRepairs_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionMaterialRepairs_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionMaterialRepairs_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionMaterialConfigurations_CreatedById",
                table: "ProductionMaterialConfigurations",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionMaterialConfigurations_LastDeletedById",
                table: "ProductionMaterialConfigurations",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionMaterialConfigurations_LastUpdatedById",
                table: "ProductionMaterialConfigurations",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionMaterialConfigurations_ProductionScheduleProductI~",
                table: "ProductionMaterialConfigurations",
                columns: new[] { "ProductionScheduleProductId", "Kind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionMaterialRepairs_ActorId",
                table: "ProductionMaterialRepairs",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionMaterialRepairs_CreatedById",
                table: "ProductionMaterialRepairs",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionMaterialRepairs_LastDeletedById",
                table: "ProductionMaterialRepairs",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionMaterialRepairs_LastUpdatedById",
                table: "ProductionMaterialRepairs",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionMaterialRepairs_ProductionScheduleProductId",
                table: "ProductionMaterialRepairs",
                column: "ProductionScheduleProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductionMaterialConfigurations");

            migrationBuilder.DropTable(
                name: "ProductionMaterialRepairs");

            migrationBuilder.DropColumn(
                name: "ContentRevision",
                table: "Requisitions");

            migrationBuilder.DropColumn(
                name: "RetiredByRepairId",
                table: "RequisitionItems");

            migrationBuilder.DropColumn(
                name: "ContentRevision",
                table: "RequisitionApprovals");

            migrationBuilder.DropColumn(
                name: "ReleasedAt",
                table: "MaterialBatchReservedQuantities");
        }
    }
}
