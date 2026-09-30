using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddFullProcedureWorkflowRevisions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_TemplateActivityRevisions_Id_TemplateActivityId",
                table: "TemplateActivityRevisions",
                columns: new[] { "Id", "TemplateActivityId" });

            migrationBuilder.CreateTable(
                name: "TemplateWorkflows",
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
                    table.PrimaryKey("PK_TemplateWorkflows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplateWorkflows_TemplateAreas_TemplateAreaId",
                        column: x => x.TemplateAreaId,
                        principalTable: "TemplateAreas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateWorkflows_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TemplateWorkflows_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TemplateWorkflows_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TemplateWorkflowRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateWorkflowId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
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
                    table.PrimaryKey("PK_TemplateWorkflowRevisions", x => x.Id);
                    table.UniqueConstraint("AK_TemplateWorkflowRevisions_Id_TemplateWorkflowId", x => new { x.Id, x.TemplateWorkflowId });
                    table.CheckConstraint("CK_TemplateWorkflowRevision_ContentHash", "\"ContentHash\" ~ '^[a-f0-9]{64}$'");
                    table.CheckConstraint("CK_TemplateWorkflowRevision_Sequence", "\"Sequence\" > 0");
                    table.CheckConstraint("CK_TemplateWorkflowRevision_Status", "\"Status\" BETWEEN 0 AND 3");
                    table.ForeignKey(
                        name: "FK_TemplateWorkflowRevisions_TemplateWorkflows_TemplateWorkflo~",
                        column: x => x.TemplateWorkflowId,
                        principalTable: "TemplateWorkflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateWorkflowRevisions_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TemplateWorkflowRevisions_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TemplateWorkflowRevisions_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TemplateWorkflowRevisions_users_PublishedById",
                        column: x => x.PublishedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateWorkflowRevisions_users_ReviewedById",
                        column: x => x.ReviewedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TemplateWorkflowNodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateWorkflowRevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    NodeType = table.Column<int>(type: "integer", nullable: false),
                    TemplateActivityId = table.Column<Guid>(type: "uuid", nullable: true),
                    TemplateActivityRevisionId = table.Column<Guid>(type: "uuid", nullable: true),
                    JoinGroupKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    WaitKind = table.Column<int>(type: "integer", nullable: true),
                    WaitConfiguration = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    HoldGroupKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ReworkTargetNodeId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReworkMaxAttempts = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateWorkflowNodes", x => x.Id);
                    table.UniqueConstraint("AK_TemplateWorkflowNodes_TemplateWorkflowRevisionId_Id", x => new { x.TemplateWorkflowRevisionId, x.Id });
                    table.CheckConstraint("CK_TemplateWorkflowNode_Order", "\"Order\" >= 0");
                    table.CheckConstraint("CK_TemplateWorkflowNode_ReworkAttempts", "\"ReworkMaxAttempts\" IS NULL OR \"ReworkMaxAttempts\" BETWEEN 1 AND 10");
                    table.CheckConstraint("CK_TemplateWorkflowNode_Type", "\"NodeType\" BETWEEN 0 AND 10");
                    table.CheckConstraint("CK_TemplateWorkflowNode_WaitKind", "\"WaitKind\" IS NULL OR \"WaitKind\" BETWEEN 0 AND 2");
                    table.ForeignKey(
                        name: "FK_TemplateWorkflowNodes_TemplateActivityRevisions_TemplateAct~",
                        columns: x => new { x.TemplateActivityRevisionId, x.TemplateActivityId },
                        principalTable: "TemplateActivityRevisions",
                        principalColumns: new[] { "Id", "TemplateActivityId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateWorkflowNodes_TemplateWorkflowNodes_TemplateWorkflo~",
                        columns: x => new { x.TemplateWorkflowRevisionId, x.ReworkTargetNodeId },
                        principalTable: "TemplateWorkflowNodes",
                        principalColumns: new[] { "TemplateWorkflowRevisionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateWorkflowNodes_TemplateWorkflowRevisions_TemplateWor~",
                        column: x => x.TemplateWorkflowRevisionId,
                        principalTable: "TemplateWorkflowRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TemplateWorkflowRevisionAudits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateWorkflowRevisionId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_TemplateWorkflowRevisionAudits", x => x.Id);
                    table.CheckConstraint("CK_TemplateWorkflowRevisionAudit_ContentHash", "\"ContentHash\" ~ '^[a-f0-9]{64}$'");
                    table.CheckConstraint("CK_TemplateWorkflowRevisionAudit_Status", "\"NewStatus\" BETWEEN 0 AND 3 AND (\"PriorStatus\" IS NULL OR \"PriorStatus\" BETWEEN 0 AND 3)");
                    table.ForeignKey(
                        name: "FK_TemplateWorkflowRevisionAudits_TemplateWorkflowRevisions_Te~",
                        column: x => x.TemplateWorkflowRevisionId,
                        principalTable: "TemplateWorkflowRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateWorkflowRevisionAudits_users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TemplateWorkflowEdges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateWorkflowRevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BranchExpression = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateWorkflowEdges", x => x.Id);
                    table.CheckConstraint("CK_TemplateWorkflowEdge_Order", "\"Order\" >= 0");
                    table.ForeignKey(
                        name: "FK_TemplateWorkflowEdges_TemplateWorkflowNodes_TemplateWorkflo~",
                        columns: x => new { x.TemplateWorkflowRevisionId, x.SourceNodeId },
                        principalTable: "TemplateWorkflowNodes",
                        principalColumns: new[] { "TemplateWorkflowRevisionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateWorkflowEdges_TemplateWorkflowNodes_TemplateWorkfl~1",
                        columns: x => new { x.TemplateWorkflowRevisionId, x.TargetNodeId },
                        principalTable: "TemplateWorkflowNodes",
                        principalColumns: new[] { "TemplateWorkflowRevisionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateWorkflowEdges_TemplateWorkflowRevisions_TemplateWor~",
                        column: x => x.TemplateWorkflowRevisionId,
                        principalTable: "TemplateWorkflowRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TemplateWorkflowNodeLayouts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateWorkflowRevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateWorkflowNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    PositionX = table.Column<double>(type: "double precision", nullable: false),
                    PositionY = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateWorkflowNodeLayouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplateWorkflowNodeLayouts_TemplateWorkflowNodes_TemplateW~",
                        columns: x => new { x.TemplateWorkflowRevisionId, x.TemplateWorkflowNodeId },
                        principalTable: "TemplateWorkflowNodes",
                        principalColumns: new[] { "TemplateWorkflowRevisionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateWorkflowNodeLayouts_TemplateWorkflowRevisions_Templ~",
                        column: x => x.TemplateWorkflowRevisionId,
                        principalTable: "TemplateWorkflowRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TemplateWorkflowEdges_TemplateWorkflowRevisionId_SourceNode~",
                table: "TemplateWorkflowEdges",
                columns: new[] { "TemplateWorkflowRevisionId", "SourceNodeId", "TargetNodeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemplateWorkflowEdges_TemplateWorkflowRevisionId_TargetNode~",
                table: "TemplateWorkflowEdges",
                columns: new[] { "TemplateWorkflowRevisionId", "TargetNodeId" });

            migrationBuilder.CreateIndex(
                name: "IX_TemplateWorkflowNodeLayouts_TemplateWorkflowRevisionId_Temp~",
                table: "TemplateWorkflowNodeLayouts",
                columns: new[] { "TemplateWorkflowRevisionId", "TemplateWorkflowNodeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemplateWorkflowNodes_TemplateActivityRevisionId_TemplateAc~",
                table: "TemplateWorkflowNodes",
                columns: new[] { "TemplateActivityRevisionId", "TemplateActivityId" });

            migrationBuilder.CreateIndex(
                name: "IX_TemplateWorkflowNodes_TemplateWorkflowRevisionId_Key",
                table: "TemplateWorkflowNodes",
                columns: new[] { "TemplateWorkflowRevisionId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemplateWorkflowNodes_TemplateWorkflowRevisionId_Order",
                table: "TemplateWorkflowNodes",
                columns: new[] { "TemplateWorkflowRevisionId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemplateWorkflowNodes_TemplateWorkflowRevisionId_ReworkTarg~",
                table: "TemplateWorkflowNodes",
                columns: new[] { "TemplateWorkflowRevisionId", "ReworkTargetNodeId" });

            migrationBuilder.CreateIndex(
                name: "IX_TemplateWorkflowRevisionAudits_ActorId",
                table: "TemplateWorkflowRevisionAudits",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateWorkflowRevisionAudits_CorrelationId",
                table: "TemplateWorkflowRevisionAudits",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateWorkflowRevisionAudits_TemplateWorkflowRevisionId_O~",
                table: "TemplateWorkflowRevisionAudits",
                columns: new[] { "TemplateWorkflowRevisionId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TemplateWorkflowRevisions_CreatedById",
                table: "TemplateWorkflowRevisions",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateWorkflowRevisions_LastDeletedById",
                table: "TemplateWorkflowRevisions",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateWorkflowRevisions_LastUpdatedById",
                table: "TemplateWorkflowRevisions",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateWorkflowRevisions_OneOpenRevision",
                table: "TemplateWorkflowRevisions",
                column: "TemplateWorkflowId",
                unique: true,
                filter: "\"Status\" IN (0, 1) AND \"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateWorkflowRevisions_OnePublishedRevision",
                table: "TemplateWorkflowRevisions",
                column: "TemplateWorkflowId",
                unique: true,
                filter: "\"Status\" = 2 AND \"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateWorkflowRevisions_PublishedById",
                table: "TemplateWorkflowRevisions",
                column: "PublishedById");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateWorkflowRevisions_ReviewedById",
                table: "TemplateWorkflowRevisions",
                column: "ReviewedById");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateWorkflowRevisions_TemplateWorkflowId_Sequence",
                table: "TemplateWorkflowRevisions",
                columns: new[] { "TemplateWorkflowId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemplateWorkflows_CreatedById",
                table: "TemplateWorkflows",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateWorkflows_LastDeletedById",
                table: "TemplateWorkflows",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateWorkflows_LastUpdatedById",
                table: "TemplateWorkflows",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateWorkflows_TemplateAreaId_PurposeId_SubjectTypeId",
                table: "TemplateWorkflows",
                columns: new[] { "TemplateAreaId", "PurposeId", "SubjectTypeId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TemplateWorkflowEdges");

            migrationBuilder.DropTable(
                name: "TemplateWorkflowNodeLayouts");

            migrationBuilder.DropTable(
                name: "TemplateWorkflowRevisionAudits");

            migrationBuilder.DropTable(
                name: "TemplateWorkflowNodes");

            migrationBuilder.DropTable(
                name: "TemplateWorkflowRevisions");

            migrationBuilder.DropTable(
                name: "TemplateWorkflows");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_TemplateActivityRevisions_Id_TemplateActivityId",
                table: "TemplateActivityRevisions");
        }
    }
}
