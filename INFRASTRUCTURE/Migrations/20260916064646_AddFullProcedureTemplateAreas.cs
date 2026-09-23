using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddFullProcedureTemplateAreas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TemplateAreas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NormalizedName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OwnerRoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewPolicyId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateAreas", x => x.Id);
                    table.CheckConstraint("CK_TemplateArea_Name", "btrim(\"Name\") <> ''");
                    table.CheckConstraint("CK_TemplateArea_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_TemplateAreas_roles_OwnerRoleId",
                        column: x => x.OwnerRoleId,
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateAreas_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TemplateAreas_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TemplateAreas_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TemplateAreaAudits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateAreaId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    SnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                    SnapshotHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateAreaAudits", x => x.Id);
                    table.CheckConstraint("CK_TemplateAreaAudit_SnapshotHash", "\"SnapshotHash\" ~ '^[a-f0-9]{64}$'");
                    table.ForeignKey(
                        name: "FK_TemplateAreaAudits_TemplateAreas_TemplateAreaId",
                        column: x => x.TemplateAreaId,
                        principalTable: "TemplateAreas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateAreaAudits_users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TemplateAreaCapabilities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateAreaId = table.Column<Guid>(type: "uuid", nullable: false),
                    CapabilityId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateAreaCapabilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplateAreaCapabilities_TemplateAreas_TemplateAreaId",
                        column: x => x.TemplateAreaId,
                        principalTable: "TemplateAreas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TemplateAreaPurposes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateAreaId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurposeId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateAreaPurposes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplateAreaPurposes_TemplateAreas_TemplateAreaId",
                        column: x => x.TemplateAreaId,
                        principalTable: "TemplateAreas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TemplateAreaRoleGrants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateAreaId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccessLevel = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateAreaRoleGrants", x => x.Id);
                    table.CheckConstraint("CK_TemplateAreaRoleGrant_AccessLevel", "\"AccessLevel\" BETWEEN 0 AND 4");
                    table.ForeignKey(
                        name: "FK_TemplateAreaRoleGrants_TemplateAreas_TemplateAreaId",
                        column: x => x.TemplateAreaId,
                        principalTable: "TemplateAreas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TemplateAreaRoleGrants_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TemplateAreaSubjectTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateAreaId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectTypeId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateAreaSubjectTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplateAreaSubjectTypes_TemplateAreas_TemplateAreaId",
                        column: x => x.TemplateAreaId,
                        principalTable: "TemplateAreas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TemplateAreaAudits_ActorId",
                table: "TemplateAreaAudits",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateAreaAudits_TemplateAreaId_Version",
                table: "TemplateAreaAudits",
                columns: new[] { "TemplateAreaId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemplateAreaCapabilities_TemplateAreaId_CapabilityId",
                table: "TemplateAreaCapabilities",
                columns: new[] { "TemplateAreaId", "CapabilityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemplateAreaPurposes_TemplateAreaId_PurposeId",
                table: "TemplateAreaPurposes",
                columns: new[] { "TemplateAreaId", "PurposeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemplateAreaRoleGrants_RoleId",
                table: "TemplateAreaRoleGrants",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateAreaRoleGrants_TemplateAreaId_RoleId",
                table: "TemplateAreaRoleGrants",
                columns: new[] { "TemplateAreaId", "RoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemplateAreas_CreatedById",
                table: "TemplateAreas",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateAreas_LastDeletedById",
                table: "TemplateAreas",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateAreas_LastUpdatedById",
                table: "TemplateAreas",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateAreas_NormalizedName",
                table: "TemplateAreas",
                column: "NormalizedName",
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateAreas_OwnerRoleId",
                table: "TemplateAreas",
                column: "OwnerRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateAreaSubjectTypes_TemplateAreaId_SubjectTypeId",
                table: "TemplateAreaSubjectTypes",
                columns: new[] { "TemplateAreaId", "SubjectTypeId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TemplateAreaAudits");

            migrationBuilder.DropTable(
                name: "TemplateAreaCapabilities");

            migrationBuilder.DropTable(
                name: "TemplateAreaPurposes");

            migrationBuilder.DropTable(
                name: "TemplateAreaRoleGrants");

            migrationBuilder.DropTable(
                name: "TemplateAreaSubjectTypes");

            migrationBuilder.DropTable(
                name: "TemplateAreas");
        }
    }
}
