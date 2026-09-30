using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddFullProcedureTemplateSharing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TemplateSharingGrants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceAreaId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetAreaId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedByAreaId = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateKind = table.Column<int>(type: "integer", nullable: false),
                    DefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RevisionContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PurposeId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    SubjectTypeId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    RequestedById = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DecidedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DecidedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedById = table.Column<Guid>(type: "uuid", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateSharingGrants", x => x.Id);
                    table.CheckConstraint("CK_TemplateSharingGrant_Areas", "\"SourceAreaId\" <> \"TargetAreaId\" AND \"RequestedByAreaId\" IN (\"SourceAreaId\", \"TargetAreaId\")");
                    table.CheckConstraint("CK_TemplateSharingGrant_ContentHash", "\"RevisionContentHash\" ~ '^[a-f0-9]{64}$'");
                    table.CheckConstraint("CK_TemplateSharingGrant_Kind", "\"TemplateKind\" BETWEEN 0 AND 4");
                    table.CheckConstraint("CK_TemplateSharingGrant_Status", "\"Status\" BETWEEN 0 AND 3");
                    table.CheckConstraint("CK_TemplateSharingGrant_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_TemplateSharingGrants_TemplateAreas_SourceAreaId",
                        column: x => x.SourceAreaId,
                        principalTable: "TemplateAreas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateSharingGrants_TemplateAreas_TargetAreaId",
                        column: x => x.TargetAreaId,
                        principalTable: "TemplateAreas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateSharingGrants_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TemplateSharingGrants_users_DecidedById",
                        column: x => x.DecidedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateSharingGrants_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TemplateSharingGrants_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TemplateSharingGrants_users_RequestedById",
                        column: x => x.RequestedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateSharingGrants_users_RevokedById",
                        column: x => x.RevokedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TemplateSharingGrantAudits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateSharingGrantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PriorStatus = table.Column<int>(type: "integer", nullable: true),
                    NewStatus = table.Column<int>(type: "integer", nullable: false),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Reason = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    SnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                    SnapshotHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateSharingGrantAudits", x => x.Id);
                    table.CheckConstraint("CK_TemplateSharingGrantAudit_SnapshotHash", "\"SnapshotHash\" ~ '^[a-f0-9]{64}$'");
                    table.CheckConstraint("CK_TemplateSharingGrantAudit_Status", "\"NewStatus\" BETWEEN 0 AND 3 AND (\"PriorStatus\" IS NULL OR \"PriorStatus\" BETWEEN 0 AND 3)");
                    table.ForeignKey(
                        name: "FK_TemplateSharingGrantAudits_TemplateSharingGrants_TemplateSh~",
                        column: x => x.TemplateSharingGrantId,
                        principalTable: "TemplateSharingGrants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateSharingGrantAudits_users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TemplateSharingGrantAudits_ActorId",
                table: "TemplateSharingGrantAudits",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateSharingGrantAudits_CorrelationId",
                table: "TemplateSharingGrantAudits",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateSharingGrantAudits_TemplateSharingGrantId_Version",
                table: "TemplateSharingGrantAudits",
                columns: new[] { "TemplateSharingGrantId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemplateSharingGrants_CreatedById",
                table: "TemplateSharingGrants",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateSharingGrants_DecidedById",
                table: "TemplateSharingGrants",
                column: "DecidedById");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateSharingGrants_LastDeletedById",
                table: "TemplateSharingGrants",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateSharingGrants_LastUpdatedById",
                table: "TemplateSharingGrants",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateSharingGrants_OneOpenGrant",
                table: "TemplateSharingGrants",
                columns: new[] { "SourceAreaId", "TargetAreaId", "TemplateKind", "DefinitionId", "RevisionId" },
                unique: true,
                filter: "\"Status\" IN (0, 1) AND \"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateSharingGrants_RequestedById",
                table: "TemplateSharingGrants",
                column: "RequestedById");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateSharingGrants_RevokedById",
                table: "TemplateSharingGrants",
                column: "RevokedById");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateSharingGrants_TargetAreaId_Status",
                table: "TemplateSharingGrants",
                columns: new[] { "TargetAreaId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TemplateSharingGrantAudits");

            migrationBuilder.DropTable(
                name: "TemplateSharingGrants");
        }
    }
}
