using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddFullProcedureActivityRevisions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_TemplateFormRevisions_Id_TemplateFormId",
                table: "TemplateFormRevisions",
                columns: new[] { "Id", "TemplateFormId" });

            migrationBuilder.CreateTable(
                name: "TemplateActivities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateAreaId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurposeId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    SubjectTypeId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplateActivities_TemplateAreas_TemplateAreaId",
                        column: x => x.TemplateAreaId,
                        principalTable: "TemplateAreas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateActivities_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TemplateActivities_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TemplateActivities_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TemplateActivityRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateActivityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Instructions = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ReviewedById = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PublishedById = table.Column<Guid>(type: "uuid", nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RetiredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateActivityRevisions", x => x.Id);
                    table.CheckConstraint("CK_TemplateActivityRevision_ContentHash", "\"ContentHash\" ~ '^[a-f0-9]{64}$'");
                    table.CheckConstraint("CK_TemplateActivityRevision_Sequence", "\"Sequence\" > 0");
                    table.CheckConstraint("CK_TemplateActivityRevision_Status", "\"Status\" BETWEEN 0 AND 3");
                    table.ForeignKey(
                        name: "FK_TemplateActivityRevisions_TemplateActivities_TemplateActivi~",
                        column: x => x.TemplateActivityId,
                        principalTable: "TemplateActivities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateActivityRevisions_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TemplateActivityRevisions_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TemplateActivityRevisions_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TemplateActivityRevisions_users_PublishedById",
                        column: x => x.PublishedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateActivityRevisions_users_ReviewedById",
                        column: x => x.ReviewedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TemplateActivityActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateActivityRevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    ActionType = table.Column<int>(type: "integer", nullable: false),
                    RequiresIndependentChecker = table.Column<bool>(type: "boolean", nullable: false),
                    RequiresApproval = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateActivityActions", x => x.Id);
                    table.CheckConstraint("CK_TemplateActivityAction_Approval", "\"ActionType\" <> 2 OR \"RequiresApproval\"");
                    table.CheckConstraint("CK_TemplateActivityAction_Order", "\"Order\" >= 0");
                    table.CheckConstraint("CK_TemplateActivityAction_Type", "\"ActionType\" BETWEEN 0 AND 3");
                    table.ForeignKey(
                        name: "FK_TemplateActivityActions_TemplateActivityRevisions_TemplateA~",
                        column: x => x.TemplateActivityRevisionId,
                        principalTable: "TemplateActivityRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TemplateActivityCompletionRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateActivityRevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    RuleType = table.Column<int>(type: "integer", nullable: false),
                    TargetKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateActivityCompletionRules", x => x.Id);
                    table.CheckConstraint("CK_TemplateActivityCompletionRule_Order", "\"Order\" >= 0");
                    table.CheckConstraint("CK_TemplateActivityCompletionRule_Type", "\"RuleType\" BETWEEN 0 AND 4");
                    table.ForeignKey(
                        name: "FK_TemplateActivityCompletionRules_TemplateActivityRevisions_T~",
                        column: x => x.TemplateActivityRevisionId,
                        principalTable: "TemplateActivityRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TemplateActivityDataBindings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateActivityRevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Direction = table.Column<int>(type: "integer", nullable: false),
                    DataType = table.Column<int>(type: "integer", nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateActivityDataBindings", x => x.Id);
                    table.CheckConstraint("CK_TemplateActivityDataBinding_Direction", "\"Direction\" BETWEEN 0 AND 1");
                    table.CheckConstraint("CK_TemplateActivityDataBinding_Order", "\"Order\" >= 0");
                    table.CheckConstraint("CK_TemplateActivityDataBinding_Type", "\"DataType\" BETWEEN 0 AND 6");
                    table.ForeignKey(
                        name: "FK_TemplateActivityDataBindings_TemplateActivityRevisions_Temp~",
                        column: x => x.TemplateActivityRevisionId,
                        principalTable: "TemplateActivityRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TemplateActivityFormBindings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateActivityRevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateFormId = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateFormRevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Usage = table.Column<int>(type: "integer", nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateActivityFormBindings", x => x.Id);
                    table.CheckConstraint("CK_TemplateActivityFormBinding_Order", "\"Order\" >= 0");
                    table.CheckConstraint("CK_TemplateActivityFormBinding_Usage", "\"Usage\" BETWEEN 0 AND 2");
                    table.ForeignKey(
                        name: "FK_TemplateActivityFormBindings_TemplateActivityRevisions_Temp~",
                        column: x => x.TemplateActivityRevisionId,
                        principalTable: "TemplateActivityRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateActivityFormBindings_TemplateFormRevisions_Template~",
                        columns: x => new { x.TemplateFormRevisionId, x.TemplateFormId },
                        principalTable: "TemplateFormRevisions",
                        principalColumns: new[] { "Id", "TemplateFormId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TemplateActivityResources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateActivityRevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CapabilityId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateActivityResources", x => x.Id);
                    table.CheckConstraint("CK_TemplateActivityResource_Order", "\"Order\" >= 0");
                    table.ForeignKey(
                        name: "FK_TemplateActivityResources_TemplateActivityRevisions_Templat~",
                        column: x => x.TemplateActivityRevisionId,
                        principalTable: "TemplateActivityRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TemplateActivityRevisionAudits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateActivityRevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PriorStatus = table.Column<int>(type: "integer", nullable: true),
                    NewStatus = table.Column<int>(type: "integer", nullable: false),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Reason = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateActivityRevisionAudits", x => x.Id);
                    table.CheckConstraint("CK_TemplateActivityRevisionAudit_ContentHash", "\"ContentHash\" ~ '^[a-f0-9]{64}$'");
                    table.CheckConstraint("CK_TemplateActivityRevisionAudit_Status", "\"NewStatus\" BETWEEN 0 AND 3 AND (\"PriorStatus\" IS NULL OR \"PriorStatus\" BETWEEN 0 AND 3)");
                    table.ForeignKey(
                        name: "FK_TemplateActivityRevisionAudits_TemplateActivityRevisions_Te~",
                        column: x => x.TemplateActivityRevisionId,
                        principalTable: "TemplateActivityRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateActivityRevisionAudits_users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TemplateActivityActionRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateActivityActionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleKind = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateActivityActionRoles", x => x.Id);
                    table.CheckConstraint("CK_TemplateActivityActionRole_Kind", "\"RoleKind\" BETWEEN 0 AND 2");
                    table.ForeignKey(
                        name: "FK_TemplateActivityActionRoles_TemplateActivityActions_Templat~",
                        column: x => x.TemplateActivityActionId,
                        principalTable: "TemplateActivityActions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateActivityActionRoles_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TemplateActivities_CreatedById",
                table: "TemplateActivities",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateActivities_LastDeletedById",
                table: "TemplateActivities",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateActivities_LastUpdatedById",
                table: "TemplateActivities",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateActivities_TemplateAreaId_PurposeId_SubjectTypeId",
                table: "TemplateActivities",
                columns: new[] { "TemplateAreaId", "PurposeId", "SubjectTypeId" });

            migrationBuilder.CreateIndex(
                name: "IX_TemplateActivityActionRoles_RoleId",
                table: "TemplateActivityActionRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateActivityActionRoles_TemplateActivityActionId_RoleId~",
                table: "TemplateActivityActionRoles",
                columns: new[] { "TemplateActivityActionId", "RoleId", "RoleKind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemplateActivityActions_TemplateActivityRevisionId_Key",
                table: "TemplateActivityActions",
                columns: new[] { "TemplateActivityRevisionId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemplateActivityActions_TemplateActivityRevisionId_Order",
                table: "TemplateActivityActions",
                columns: new[] { "TemplateActivityRevisionId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemplateActivityCompletionRules_TemplateActivityRevisionId_~",
                table: "TemplateActivityCompletionRules",
                columns: new[] { "TemplateActivityRevisionId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemplateActivityCompletionRules_TemplateActivityRevisionId~1",
                table: "TemplateActivityCompletionRules",
                columns: new[] { "TemplateActivityRevisionId", "RuleType", "TargetKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemplateActivityDataBindings_TemplateActivityRevisionId_Key",
                table: "TemplateActivityDataBindings",
                columns: new[] { "TemplateActivityRevisionId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemplateActivityDataBindings_TemplateActivityRevisionId_Ord~",
                table: "TemplateActivityDataBindings",
                columns: new[] { "TemplateActivityRevisionId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemplateActivityFormBindings_TemplateActivityRevisionId_Key",
                table: "TemplateActivityFormBindings",
                columns: new[] { "TemplateActivityRevisionId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemplateActivityFormBindings_TemplateActivityRevisionId_Ord~",
                table: "TemplateActivityFormBindings",
                columns: new[] { "TemplateActivityRevisionId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemplateActivityFormBindings_TemplateActivityRevisionId_Tem~",
                table: "TemplateActivityFormBindings",
                columns: new[] { "TemplateActivityRevisionId", "TemplateFormId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemplateActivityFormBindings_TemplateFormRevisionId_Templat~",
                table: "TemplateActivityFormBindings",
                columns: new[] { "TemplateFormRevisionId", "TemplateFormId" });

            migrationBuilder.CreateIndex(
                name: "IX_TemplateActivityResources_TemplateActivityRevisionId_Capabi~",
                table: "TemplateActivityResources",
                columns: new[] { "TemplateActivityRevisionId", "CapabilityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemplateActivityResources_TemplateActivityRevisionId_Order",
                table: "TemplateActivityResources",
                columns: new[] { "TemplateActivityRevisionId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemplateActivityRevisionAudits_ActorId",
                table: "TemplateActivityRevisionAudits",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateActivityRevisionAudits_CorrelationId",
                table: "TemplateActivityRevisionAudits",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateActivityRevisionAudits_TemplateActivityRevisionId_O~",
                table: "TemplateActivityRevisionAudits",
                columns: new[] { "TemplateActivityRevisionId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TemplateActivityRevisions_CreatedById",
                table: "TemplateActivityRevisions",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateActivityRevisions_LastDeletedById",
                table: "TemplateActivityRevisions",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateActivityRevisions_LastUpdatedById",
                table: "TemplateActivityRevisions",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateActivityRevisions_OneOpenRevision",
                table: "TemplateActivityRevisions",
                column: "TemplateActivityId",
                unique: true,
                filter: "\"Status\" IN (0, 1) AND \"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateActivityRevisions_OnePublishedRevision",
                table: "TemplateActivityRevisions",
                column: "TemplateActivityId",
                unique: true,
                filter: "\"Status\" = 2 AND \"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateActivityRevisions_PublishedById",
                table: "TemplateActivityRevisions",
                column: "PublishedById");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateActivityRevisions_ReviewedById",
                table: "TemplateActivityRevisions",
                column: "ReviewedById");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateActivityRevisions_TemplateActivityId_Sequence",
                table: "TemplateActivityRevisions",
                columns: new[] { "TemplateActivityId", "Sequence" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TemplateActivityActionRoles");

            migrationBuilder.DropTable(
                name: "TemplateActivityCompletionRules");

            migrationBuilder.DropTable(
                name: "TemplateActivityDataBindings");

            migrationBuilder.DropTable(
                name: "TemplateActivityFormBindings");

            migrationBuilder.DropTable(
                name: "TemplateActivityResources");

            migrationBuilder.DropTable(
                name: "TemplateActivityRevisionAudits");

            migrationBuilder.DropTable(
                name: "TemplateActivityActions");

            migrationBuilder.DropTable(
                name: "TemplateActivityRevisions");

            migrationBuilder.DropTable(
                name: "TemplateActivities");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_TemplateFormRevisions_Id_TemplateFormId",
                table: "TemplateFormRevisions");

        }
    }
}
