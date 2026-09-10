using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddRndStabilityStudyModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RndStabilityChambers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ConditionType = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    TargetTemperature = table.Column<decimal>(type: "numeric", nullable: false),
                    TargetHumidity = table.Column<decimal>(type: "numeric", nullable: true),
                    CalibrationDueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastCalibratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RndStabilityChambers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RndStabilityChambers_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RndStabilityChambers_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RndStabilityChambers_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RndStabilityStudies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RndProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    RndTrialBatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    RndStabilityChamberId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProtocolFormId = table.Column<Guid>(type: "uuid", nullable: true),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RndStabilityStudies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RndStabilityStudies_Forms_ProtocolFormId",
                        column: x => x.ProtocolFormId,
                        principalTable: "Forms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RndStabilityStudies_RndProjects_RndProjectId",
                        column: x => x.RndProjectId,
                        principalTable: "RndProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RndStabilityStudies_RndStabilityChambers_RndStabilityChambe~",
                        column: x => x.RndStabilityChamberId,
                        principalTable: "RndStabilityChambers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RndStabilityStudies_RndTrialBatches_RndTrialBatchId",
                        column: x => x.RndTrialBatchId,
                        principalTable: "RndTrialBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RndStabilityStudies_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RndStabilityStudies_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RndStabilityStudies_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RndStabilityPullPoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RndStabilityStudyId = table.Column<Guid>(type: "uuid", nullable: false),
                    TimePointMonths = table.Column<int>(type: "integer", nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PulledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PulledById = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ResultsSummary = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RndStabilityPullPoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RndStabilityPullPoints_RndStabilityStudies_RndStabilityStud~",
                        column: x => x.RndStabilityStudyId,
                        principalTable: "RndStabilityStudies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RndStabilityPullPoints_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RndStabilityPullPoints_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RndStabilityPullPoints_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RndStabilityPullPoints_users_PulledById",
                        column: x => x.PulledById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RndStabilityChambers_CreatedById",
                table: "RndStabilityChambers",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RndStabilityChambers_LastDeletedById",
                table: "RndStabilityChambers",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_RndStabilityChambers_LastUpdatedById",
                table: "RndStabilityChambers",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RndStabilityPullPoints_CreatedById",
                table: "RndStabilityPullPoints",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RndStabilityPullPoints_LastDeletedById",
                table: "RndStabilityPullPoints",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_RndStabilityPullPoints_LastUpdatedById",
                table: "RndStabilityPullPoints",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RndStabilityPullPoints_PulledById",
                table: "RndStabilityPullPoints",
                column: "PulledById");

            migrationBuilder.CreateIndex(
                name: "IX_RndStabilityPullPoints_RndStabilityStudyId",
                table: "RndStabilityPullPoints",
                column: "RndStabilityStudyId");

            migrationBuilder.CreateIndex(
                name: "IX_RndStabilityStudies_CreatedById",
                table: "RndStabilityStudies",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RndStabilityStudies_LastDeletedById",
                table: "RndStabilityStudies",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_RndStabilityStudies_LastUpdatedById",
                table: "RndStabilityStudies",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RndStabilityStudies_ProtocolFormId",
                table: "RndStabilityStudies",
                column: "ProtocolFormId");

            migrationBuilder.CreateIndex(
                name: "IX_RndStabilityStudies_RndProjectId",
                table: "RndStabilityStudies",
                column: "RndProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_RndStabilityStudies_RndStabilityChamberId",
                table: "RndStabilityStudies",
                column: "RndStabilityChamberId");

            migrationBuilder.CreateIndex(
                name: "IX_RndStabilityStudies_RndTrialBatchId",
                table: "RndStabilityStudies",
                column: "RndTrialBatchId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RndStabilityPullPoints");

            migrationBuilder.DropTable(
                name: "RndStabilityStudies");

            migrationBuilder.DropTable(
                name: "RndStabilityChambers");
        }
    }
}
