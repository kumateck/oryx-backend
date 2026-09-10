using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddRndTechnologyTransferModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RndTechnologyTransfers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RndProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    RndFormulationId = table.Column<Guid>(type: "uuid", nullable: false),
                    GapAnalysisFormId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    BillOfMaterialId = table.Column<Guid>(type: "uuid", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedById = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RndTechnologyTransfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RndTechnologyTransfers_Forms_GapAnalysisFormId",
                        column: x => x.GapAnalysisFormId,
                        principalTable: "Forms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RndTechnologyTransfers_RndFormulations_RndFormulationId",
                        column: x => x.RndFormulationId,
                        principalTable: "RndFormulations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RndTechnologyTransfers_RndProjects_RndProjectId",
                        column: x => x.RndProjectId,
                        principalTable: "RndProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RndTechnologyTransfers_users_CompletedById",
                        column: x => x.CompletedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RndTechnologyTransfers_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RndTechnologyTransfers_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RndTechnologyTransfers_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_RndTechnologyTransfers_CompletedById",
                table: "RndTechnologyTransfers",
                column: "CompletedById");

            migrationBuilder.CreateIndex(
                name: "IX_RndTechnologyTransfers_CreatedById",
                table: "RndTechnologyTransfers",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RndTechnologyTransfers_GapAnalysisFormId",
                table: "RndTechnologyTransfers",
                column: "GapAnalysisFormId");

            migrationBuilder.CreateIndex(
                name: "IX_RndTechnologyTransfers_LastDeletedById",
                table: "RndTechnologyTransfers",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_RndTechnologyTransfers_LastUpdatedById",
                table: "RndTechnologyTransfers",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RndTechnologyTransfers_RndFormulationId",
                table: "RndTechnologyTransfers",
                column: "RndFormulationId");

            migrationBuilder.CreateIndex(
                name: "IX_RndTechnologyTransfers_RndProjectId",
                table: "RndTechnologyTransfers",
                column: "RndProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RndTechnologyTransfers");
        }
    }
}
