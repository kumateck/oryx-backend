using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddQcWorksheetTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QcApprovals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovalId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovalRound = table.Column<int>(type: "integer", nullable: false),
                    ReauthConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: true),
                    Required = table.Column<bool>(type: "boolean", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    StageStartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ApprovalTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedById = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActivatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Comments = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QcApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QcApprovals_Approvals_ApprovalId",
                        column: x => x.ApprovalId,
                        principalTable: "Approvals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QcApprovals_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QcApprovals_users_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QcApprovals_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QcStandardTestProcedures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Approved = table.Column<bool>(type: "boolean", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Area = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    SupersedesId = table.Column<Guid>(type: "uuid", nullable: true),
                    EffectiveDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IssueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Purpose = table.Column<string>(type: "text", nullable: true),
                    Scope = table.Column<string>(type: "text", nullable: true),
                    Responsibility = table.Column<string>(type: "text", nullable: true),
                    Accountability = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_QcStandardTestProcedures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QcStandardTestProcedures_QcStandardTestProcedures_Supersede~",
                        column: x => x.SupersedesId,
                        principalTable: "QcStandardTestProcedures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QcStandardTestProcedures_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QcStandardTestProcedures_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QcStandardTestProcedures_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "QcStpSteps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StandardTestProcedureId = table.Column<Guid>(type: "uuid", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Instruction = table.Column<string>(type: "text", nullable: false),
                    ReferencedStpId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QcStpSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QcStpSteps_QcStandardTestProcedures_ReferencedStpId",
                        column: x => x.ReferencedStpId,
                        principalTable: "QcStandardTestProcedures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QcStpSteps_QcStandardTestProcedures_StandardTestProcedureId",
                        column: x => x.StandardTestProcedureId,
                        principalTable: "QcStandardTestProcedures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QcStpSteps_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QcStpSteps_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QcStpSteps_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "QcWorksheetTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Approved = table.Column<bool>(type: "boolean", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Department = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    StpId = table.Column<Guid>(type: "uuid", nullable: true),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    SupersedesId = table.Column<Guid>(type: "uuid", nullable: true),
                    EffectiveDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_QcWorksheetTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QcWorksheetTemplates_QcStandardTestProcedures_StpId",
                        column: x => x.StpId,
                        principalTable: "QcStandardTestProcedures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QcWorksheetTemplates_QcWorksheetTemplates_SupersedesId",
                        column: x => x.SupersedesId,
                        principalTable: "QcWorksheetTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QcWorksheetTemplates_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QcWorksheetTemplates_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QcWorksheetTemplates_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "QcWorksheetSections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorksheetTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    InstrumentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QcWorksheetSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QcWorksheetSections_QcWorksheetTemplates_WorksheetTemplateId",
                        column: x => x.WorksheetTemplateId,
                        principalTable: "QcWorksheetTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QcWorksheetSections_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QcWorksheetSections_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QcWorksheetSections_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "QcWorksheetFields",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorksheetSectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    FieldKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Label = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Mode = table.Column<int>(type: "integer", nullable: false),
                    Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Analyte = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ConstantValue = table.Column<string>(type: "text", nullable: true),
                    FormulaExpression = table.Column<string>(type: "text", nullable: true),
                    ColumnDefinitions = table.Column<string>(type: "text", nullable: true),
                    ReferencedResultSourceTemplateId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReferencedResultSourceFieldKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ReferencedResultResolutionFieldKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QcWorksheetFields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QcWorksheetFields_QcWorksheetSections_WorksheetSectionId",
                        column: x => x.WorksheetSectionId,
                        principalTable: "QcWorksheetSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QcWorksheetFields_QcWorksheetTemplates_ReferencedResultSour~",
                        column: x => x.ReferencedResultSourceTemplateId,
                        principalTable: "QcWorksheetTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QcWorksheetFields_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QcWorksheetFields_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QcWorksheetFields_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "QcWorksheetFieldRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorksheetFieldId = table.Column<Guid>(type: "uuid", nullable: false),
                    RevisionNumber = table.Column<int>(type: "integer", nullable: false),
                    FieldKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Label = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Mode = table.Column<int>(type: "integer", nullable: false),
                    Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Analyte = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ConstantValue = table.Column<string>(type: "text", nullable: true),
                    FormulaExpression = table.Column<string>(type: "text", nullable: true),
                    ColumnDefinitions = table.Column<string>(type: "text", nullable: true),
                    ReferencedResultSourceTemplateId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReferencedResultSourceFieldKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ReferencedResultResolutionFieldKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QcWorksheetFieldRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QcWorksheetFieldRevisions_QcWorksheetFields_WorksheetFieldId",
                        column: x => x.WorksheetFieldId,
                        principalTable: "QcWorksheetFields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QcWorksheetFieldRevisions_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QcWorksheetFieldRevisions_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QcWorksheetFieldRevisions_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_QcApprovals_ApprovalId_EntityType_EntityId_ApprovalRound_Or~",
                table: "QcApprovals",
                columns: new[] { "ApprovalId", "EntityType", "EntityId", "ApprovalRound", "Order", "UserId", "RoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QcApprovals_ApprovedById",
                table: "QcApprovals",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcApprovals_EntityType_EntityId",
                table: "QcApprovals",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_QcApprovals_RoleId",
                table: "QcApprovals",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_QcApprovals_UserId",
                table: "QcApprovals",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_QcStandardTestProcedures_Code",
                table: "QcStandardTestProcedures",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_QcStandardTestProcedures_CreatedById",
                table: "QcStandardTestProcedures",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcStandardTestProcedures_LastDeletedById",
                table: "QcStandardTestProcedures",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcStandardTestProcedures_LastUpdatedById",
                table: "QcStandardTestProcedures",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcStandardTestProcedures_Status",
                table: "QcStandardTestProcedures",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_QcStandardTestProcedures_SupersedesId",
                table: "QcStandardTestProcedures",
                column: "SupersedesId");

            migrationBuilder.CreateIndex(
                name: "IX_QcStpSteps_CreatedById",
                table: "QcStpSteps",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcStpSteps_LastDeletedById",
                table: "QcStpSteps",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcStpSteps_LastUpdatedById",
                table: "QcStpSteps",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcStpSteps_ReferencedStpId",
                table: "QcStpSteps",
                column: "ReferencedStpId");

            migrationBuilder.CreateIndex(
                name: "IX_QcStpSteps_StandardTestProcedureId_Order",
                table: "QcStpSteps",
                columns: new[] { "StandardTestProcedureId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_QcWorksheetFieldRevisions_CreatedById",
                table: "QcWorksheetFieldRevisions",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcWorksheetFieldRevisions_LastDeletedById",
                table: "QcWorksheetFieldRevisions",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcWorksheetFieldRevisions_LastUpdatedById",
                table: "QcWorksheetFieldRevisions",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcWorksheetFieldRevisions_WorksheetFieldId_RevisionNumber",
                table: "QcWorksheetFieldRevisions",
                columns: new[] { "WorksheetFieldId", "RevisionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QcWorksheetFields_CreatedById",
                table: "QcWorksheetFields",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcWorksheetFields_LastDeletedById",
                table: "QcWorksheetFields",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcWorksheetFields_LastUpdatedById",
                table: "QcWorksheetFields",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcWorksheetFields_ReferencedResultSourceTemplateId",
                table: "QcWorksheetFields",
                column: "ReferencedResultSourceTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_QcWorksheetFields_WorksheetSectionId_FieldKey",
                table: "QcWorksheetFields",
                columns: new[] { "WorksheetSectionId", "FieldKey" });

            migrationBuilder.CreateIndex(
                name: "IX_QcWorksheetSections_CreatedById",
                table: "QcWorksheetSections",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcWorksheetSections_LastDeletedById",
                table: "QcWorksheetSections",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcWorksheetSections_LastUpdatedById",
                table: "QcWorksheetSections",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcWorksheetSections_WorksheetTemplateId_Order",
                table: "QcWorksheetSections",
                columns: new[] { "WorksheetTemplateId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_QcWorksheetTemplates_Code",
                table: "QcWorksheetTemplates",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_QcWorksheetTemplates_CreatedById",
                table: "QcWorksheetTemplates",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcWorksheetTemplates_LastDeletedById",
                table: "QcWorksheetTemplates",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcWorksheetTemplates_LastUpdatedById",
                table: "QcWorksheetTemplates",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcWorksheetTemplates_Status",
                table: "QcWorksheetTemplates",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_QcWorksheetTemplates_StpId",
                table: "QcWorksheetTemplates",
                column: "StpId");

            migrationBuilder.CreateIndex(
                name: "IX_QcWorksheetTemplates_SupersedesId",
                table: "QcWorksheetTemplates",
                column: "SupersedesId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QcApprovals");

            migrationBuilder.DropTable(
                name: "QcStpSteps");

            migrationBuilder.DropTable(
                name: "QcWorksheetFieldRevisions");

            migrationBuilder.DropTable(
                name: "QcWorksheetFields");

            migrationBuilder.DropTable(
                name: "QcWorksheetSections");

            migrationBuilder.DropTable(
                name: "QcWorksheetTemplates");

            migrationBuilder.DropTable(
                name: "QcStandardTestProcedures");
        }
    }
}
