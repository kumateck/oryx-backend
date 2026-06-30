using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddOosAndSpecificationReference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Reference",
                table: "ProductSpecifications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Reference",
                table: "MaterialSpecifications",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OosInvestigations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AnalyticalTestRequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    MaterialBatchId = table.Column<Guid>(type: "uuid", nullable: true),
                    CoaNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ProductOrMaterialName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BatchNumber = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RootCauseAnalysis = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CorrectiveActions = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    PreventiveActions = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    InvestigationDetails = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SubmittedToQaAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SubmittedById = table.Column<Guid>(type: "uuid", nullable: true),
                    QaReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    QaReviewerId = table.Column<Guid>(type: "uuid", nullable: true),
                    QaReviewComments = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OosInvestigations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OosInvestigations_AnalyticalTestRequests_AnalyticalTestRequ~",
                        column: x => x.AnalyticalTestRequestId,
                        principalTable: "AnalyticalTestRequests",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OosInvestigations_MaterialBatches_MaterialBatchId",
                        column: x => x.MaterialBatchId,
                        principalTable: "MaterialBatches",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OosInvestigations_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OosInvestigations_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OosInvestigations_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OosInvestigations_users_QaReviewerId",
                        column: x => x.QaReviewerId,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OosInvestigations_users_SubmittedById",
                        column: x => x.SubmittedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_OosInvestigations_AnalyticalTestRequestId",
                table: "OosInvestigations",
                column: "AnalyticalTestRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_OosInvestigations_CreatedById",
                table: "OosInvestigations",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_OosInvestigations_LastDeletedById",
                table: "OosInvestigations",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_OosInvestigations_LastUpdatedById",
                table: "OosInvestigations",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_OosInvestigations_MaterialBatchId",
                table: "OosInvestigations",
                column: "MaterialBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_OosInvestigations_QaReviewerId",
                table: "OosInvestigations",
                column: "QaReviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_OosInvestigations_SubmittedById",
                table: "OosInvestigations",
                column: "SubmittedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OosInvestigations");

            migrationBuilder.DropColumn(
                name: "Reference",
                table: "ProductSpecifications");

            migrationBuilder.DropColumn(
                name: "Reference",
                table: "MaterialSpecifications");
        }
    }
}
