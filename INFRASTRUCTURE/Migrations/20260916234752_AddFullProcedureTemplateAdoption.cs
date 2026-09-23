using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddFullProcedureTemplateAdoption : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TemplateAdoptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateSharingGrantId = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateKind = table.Column<int>(type: "integer", nullable: false),
                    SourceDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceRevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    GrantVersion = table.Column<int>(type: "integer", nullable: false),
                    TargetAreaId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetRevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DependencyMappingsJson = table.Column<string>(type: "jsonb", nullable: false),
                    RoleMappingsJson = table.Column<string>(type: "jsonb", nullable: false),
                    Reason = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    SnapshotHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AdoptedById = table.Column<Guid>(type: "uuid", nullable: false),
                    AdoptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateAdoptions", x => x.Id);
                    table.CheckConstraint("CK_TemplateAdoption_GrantVersion", "\"GrantVersion\" > 0");
                    table.CheckConstraint("CK_TemplateAdoption_Kind", "\"TemplateKind\" BETWEEN 0 AND 4");
                    table.CheckConstraint("CK_TemplateAdoption_SnapshotHash", "\"SnapshotHash\" ~ '^[a-f0-9]{64}$'");
                    table.CheckConstraint("CK_TemplateAdoption_SourceHash", "\"SourceContentHash\" ~ '^[a-f0-9]{64}$'");
                    table.CheckConstraint("CK_TemplateAdoption_TargetHash", "\"TargetContentHash\" ~ '^[a-f0-9]{64}$'");
                    table.ForeignKey(
                        name: "FK_TemplateAdoptions_TemplateAreas_TargetAreaId",
                        column: x => x.TargetAreaId,
                        principalTable: "TemplateAreas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateAdoptions_TemplateSharingGrants_TemplateSharingGran~",
                        column: x => x.TemplateSharingGrantId,
                        principalTable: "TemplateSharingGrants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateAdoptions_users_AdoptedById",
                        column: x => x.AdoptedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateAdoptions_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TemplateAdoptions_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TemplateAdoptions_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_TemplateAdoptions_AdoptedById",
                table: "TemplateAdoptions",
                column: "AdoptedById");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateAdoptions_CorrelationId",
                table: "TemplateAdoptions",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateAdoptions_CreatedById",
                table: "TemplateAdoptions",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateAdoptions_LastDeletedById",
                table: "TemplateAdoptions",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateAdoptions_LastUpdatedById",
                table: "TemplateAdoptions",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateAdoptions_TargetAreaId_TemplateKind_AdoptedAt",
                table: "TemplateAdoptions",
                columns: new[] { "TargetAreaId", "TemplateKind", "AdoptedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TemplateAdoptions_TemplateSharingGrantId",
                table: "TemplateAdoptions",
                column: "TemplateSharingGrantId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TemplateAdoptions");
        }
    }
}
