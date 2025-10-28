using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddPreSample : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PreSampleChecklists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MaterialBatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    GrnId = table.Column<Guid>(type: "uuid", nullable: false),
                    TemperatureCondition = table.Column<decimal>(type: "numeric", nullable: false),
                    EnvironmentalConditionRh = table.Column<decimal>(type: "numeric", nullable: false),
                    PackingCleanliness = table.Column<int>(type: "integer", nullable: false),
                    PackingStyles = table.Column<List<string>>(type: "text[]", nullable: true),
                    QuarantinedLabel = table.Column<int>(type: "integer", nullable: false),
                    PharmacopoeiaStatus = table.Column<List<string>>(type: "text[]", nullable: true),
                    ManufacturersNameMentioned = table.Column<bool>(type: "boolean", nullable: false),
                    BatchNumber = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    StorageCondition = table.Column<int>(type: "integer", nullable: false),
                    NoOfContainer = table.Column<int>(type: "integer", nullable: false),
                    AnyContainerDamaged = table.Column<bool>(type: "boolean", nullable: false),
                    AnyOtherRemarks = table.Column<string>(type: "character varying(1000000)", maxLength: 1000000, nullable: true),
                    PhysicalAppearance = table.Column<int>(type: "integer", nullable: false),
                    ManufacturerSeal = table.Column<int>(type: "integer", nullable: false),
                    PresenceOfLumps = table.Column<int>(type: "integer", nullable: false),
                    AnyNonCharacteristicOdour = table.Column<int>(type: "integer", nullable: false),
                    HeterogeneityWithinSameContainer = table.Column<int>(type: "integer", nullable: false),
                    HeterogeneityBetweenDifferentContainers = table.Column<int>(type: "integer", nullable: false),
                    DoneById = table.Column<Guid>(type: "uuid", nullable: false),
                    CheckedById = table.Column<Guid>(type: "uuid", nullable: false),
                    FormStatus = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreSampleChecklists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PreSampleChecklists_Grns_GrnId",
                        column: x => x.GrnId,
                        principalTable: "Grns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PreSampleChecklists_MaterialBatches_MaterialBatchId",
                        column: x => x.MaterialBatchId,
                        principalTable: "MaterialBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PreSampleChecklists_users_CheckedById",
                        column: x => x.CheckedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PreSampleChecklists_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PreSampleChecklists_users_DoneById",
                        column: x => x.DoneById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PreSampleChecklists_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PreSampleChecklists_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PreSampleChecklists_CheckedById",
                table: "PreSampleChecklists",
                column: "CheckedById");

            migrationBuilder.CreateIndex(
                name: "IX_PreSampleChecklists_CreatedById",
                table: "PreSampleChecklists",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_PreSampleChecklists_DoneById",
                table: "PreSampleChecklists",
                column: "DoneById");

            migrationBuilder.CreateIndex(
                name: "IX_PreSampleChecklists_GrnId",
                table: "PreSampleChecklists",
                column: "GrnId");

            migrationBuilder.CreateIndex(
                name: "IX_PreSampleChecklists_LastDeletedById",
                table: "PreSampleChecklists",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_PreSampleChecklists_LastUpdatedById",
                table: "PreSampleChecklists",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_PreSampleChecklists_MaterialBatchId",
                table: "PreSampleChecklists",
                column: "MaterialBatchId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PreSampleChecklists");
        }
    }
}
