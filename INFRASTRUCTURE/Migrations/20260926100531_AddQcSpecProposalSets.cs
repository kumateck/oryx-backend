using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddQcSpecProposalSets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QcSpecProposalSets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Family = table.Column<int>(type: "integer", nullable: false),
                    SourceFileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    WorksheetTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SpecificationCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ProposalJson = table.Column<string>(type: "jsonb", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AppliedSpecificationId = table.Column<Guid>(type: "uuid", nullable: true),
                    DismissReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QcSpecProposalSets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QcSpecProposalSets_QcSpecifications_AppliedSpecificationId",
                        column: x => x.AppliedSpecificationId,
                        principalTable: "QcSpecifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QcSpecProposalSets_QcWorksheetTemplates_WorksheetTemplateId",
                        column: x => x.WorksheetTemplateId,
                        principalTable: "QcWorksheetTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QcSpecProposalSets_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QcSpecProposalSets_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QcSpecProposalSets_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_QcSpecProposalSets_AppliedSpecificationId",
                table: "QcSpecProposalSets",
                column: "AppliedSpecificationId");

            migrationBuilder.CreateIndex(
                name: "IX_QcSpecProposalSets_CreatedById",
                table: "QcSpecProposalSets",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcSpecProposalSets_LastDeletedById",
                table: "QcSpecProposalSets",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcSpecProposalSets_LastUpdatedById",
                table: "QcSpecProposalSets",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcSpecProposalSets_Status_Family",
                table: "QcSpecProposalSets",
                columns: new[] { "Status", "Family" });

            migrationBuilder.CreateIndex(
                name: "IX_QcSpecProposalSets_WorksheetTemplateId",
                table: "QcSpecProposalSets",
                column: "WorksheetTemplateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QcSpecProposalSets");
        }
    }
}
