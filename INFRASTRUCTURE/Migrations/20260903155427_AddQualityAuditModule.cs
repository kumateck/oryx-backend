using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddQualityAuditModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditChecklistTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditChecklistTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditChecklistTemplates_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AuditChecklistTemplates_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AuditChecklistTemplates_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AuditChecklistTemplateItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AuditChecklistTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    SectionName = table.Column<string>(type: "text", nullable: true),
                    QuestionText = table.Column<string>(type: "text", nullable: true),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_AuditChecklistTemplateItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditChecklistTemplateItems_AuditChecklistTemplates_AuditCh~",
                        column: x => x.AuditChecklistTemplateId,
                        principalTable: "AuditChecklistTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AuditChecklistTemplateItems_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AuditChecklistTemplateItems_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AuditChecklistTemplateItems_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "QualityAudits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AuditNumber = table.Column<string>(type: "text", nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    FocusArea = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: true),
                    Scope = table.Column<string>(type: "text", nullable: true),
                    ObjectiveNotes = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ScheduledStartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ScheduledEndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActualStartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ActualEndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LeadAuditorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: true),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: true),
                    ChecklistTemplateId = table.Column<Guid>(type: "uuid", nullable: true),
                    ClosingMeetingNotes = table.Column<string>(type: "text", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClosedById = table.Column<Guid>(type: "uuid", nullable: true),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    VerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VerifiedById = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QualityAudits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QualityAudits_AuditChecklistTemplates_ChecklistTemplateId",
                        column: x => x.ChecklistTemplateId,
                        principalTable: "AuditChecklistTemplates",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QualityAudits_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QualityAudits_ProductionOrders_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalTable: "ProductionOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QualityAudits_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QualityAudits_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QualityAudits_users_ClosedById",
                        column: x => x.ClosedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QualityAudits_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QualityAudits_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QualityAudits_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QualityAudits_users_LeadAuditorId",
                        column: x => x.LeadAuditorId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QualityAudits_users_VerifiedById",
                        column: x => x.VerifiedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AuditChecklistResponses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QualityAuditId = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    AdHocQuestionText = table.Column<string>(type: "text", nullable: true),
                    ResponseStatus = table.Column<int>(type: "integer", nullable: false),
                    Comments = table.Column<string>(type: "text", nullable: true),
                    RespondedById = table.Column<Guid>(type: "uuid", nullable: true),
                    RespondedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditChecklistResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditChecklistResponses_AuditChecklistTemplateItems_Templat~",
                        column: x => x.TemplateItemId,
                        principalTable: "AuditChecklistTemplateItems",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AuditChecklistResponses_QualityAudits_QualityAuditId",
                        column: x => x.QualityAuditId,
                        principalTable: "QualityAudits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AuditChecklistResponses_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AuditChecklistResponses_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AuditChecklistResponses_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AuditChecklistResponses_users_RespondedById",
                        column: x => x.RespondedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "QualityAuditTeamMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QualityAuditId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QualityAuditTeamMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QualityAuditTeamMembers_QualityAudits_QualityAuditId",
                        column: x => x.QualityAuditId,
                        principalTable: "QualityAudits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QualityAuditTeamMembers_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QualityAuditTeamMembers_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QualityAuditTeamMembers_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QualityAuditTeamMembers_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AuditFindings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QualityAuditId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChecklistResponseId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Severity = table.Column<int>(type: "integer", nullable: false),
                    AreaOrClause = table.Column<string>(type: "text", nullable: true),
                    RaisedById = table.Column<Guid>(type: "uuid", nullable: true),
                    RaisedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("PK_AuditFindings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditFindings_AuditChecklistResponses_ChecklistResponseId",
                        column: x => x.ChecklistResponseId,
                        principalTable: "AuditChecklistResponses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AuditFindings_QualityAudits_QualityAuditId",
                        column: x => x.QualityAuditId,
                        principalTable: "QualityAudits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AuditFindings_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AuditFindings_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AuditFindings_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AuditFindings_users_RaisedById",
                        column: x => x.RaisedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AuditCorrectiveActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AuditFindingId = table.Column<Guid>(type: "uuid", nullable: false),
                    RootCauseAnalysis = table.Column<string>(type: "text", nullable: true),
                    CorrectiveActions = table.Column<string>(type: "text", nullable: true),
                    PreventiveActions = table.Column<string>(type: "text", nullable: true),
                    ResponsiblePersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    EffectivenessCheckNotes = table.Column<string>(type: "text", nullable: true),
                    EffectivenessVerifiedById = table.Column<Guid>(type: "uuid", nullable: true),
                    EffectivenessVerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditCorrectiveActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditCorrectiveActions_AuditFindings_AuditFindingId",
                        column: x => x.AuditFindingId,
                        principalTable: "AuditFindings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AuditCorrectiveActions_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AuditCorrectiveActions_users_EffectivenessVerifiedById",
                        column: x => x.EffectivenessVerifiedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AuditCorrectiveActions_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AuditCorrectiveActions_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AuditCorrectiveActions_users_ResponsiblePersonId",
                        column: x => x.ResponsiblePersonId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditChecklistResponses_CreatedById",
                table: "AuditChecklistResponses",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_AuditChecklistResponses_LastDeletedById",
                table: "AuditChecklistResponses",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_AuditChecklistResponses_LastUpdatedById",
                table: "AuditChecklistResponses",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_AuditChecklistResponses_QualityAuditId",
                table: "AuditChecklistResponses",
                column: "QualityAuditId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditChecklistResponses_RespondedById",
                table: "AuditChecklistResponses",
                column: "RespondedById");

            migrationBuilder.CreateIndex(
                name: "IX_AuditChecklistResponses_TemplateItemId",
                table: "AuditChecklistResponses",
                column: "TemplateItemId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditChecklistTemplateItems_AuditChecklistTemplateId",
                table: "AuditChecklistTemplateItems",
                column: "AuditChecklistTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditChecklistTemplateItems_CreatedById",
                table: "AuditChecklistTemplateItems",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_AuditChecklistTemplateItems_LastDeletedById",
                table: "AuditChecklistTemplateItems",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_AuditChecklistTemplateItems_LastUpdatedById",
                table: "AuditChecklistTemplateItems",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_AuditChecklistTemplates_CreatedById",
                table: "AuditChecklistTemplates",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_AuditChecklistTemplates_LastDeletedById",
                table: "AuditChecklistTemplates",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_AuditChecklistTemplates_LastUpdatedById",
                table: "AuditChecklistTemplates",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_AuditCorrectiveActions_AuditFindingId",
                table: "AuditCorrectiveActions",
                column: "AuditFindingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditCorrectiveActions_CreatedById",
                table: "AuditCorrectiveActions",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_AuditCorrectiveActions_EffectivenessVerifiedById",
                table: "AuditCorrectiveActions",
                column: "EffectivenessVerifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_AuditCorrectiveActions_LastDeletedById",
                table: "AuditCorrectiveActions",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_AuditCorrectiveActions_LastUpdatedById",
                table: "AuditCorrectiveActions",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_AuditCorrectiveActions_ResponsiblePersonId",
                table: "AuditCorrectiveActions",
                column: "ResponsiblePersonId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditFindings_ChecklistResponseId",
                table: "AuditFindings",
                column: "ChecklistResponseId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditFindings_CreatedById",
                table: "AuditFindings",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_AuditFindings_LastDeletedById",
                table: "AuditFindings",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_AuditFindings_LastUpdatedById",
                table: "AuditFindings",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_AuditFindings_QualityAuditId",
                table: "AuditFindings",
                column: "QualityAuditId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditFindings_RaisedById",
                table: "AuditFindings",
                column: "RaisedById");

            migrationBuilder.CreateIndex(
                name: "IX_QualityAudits_ChecklistTemplateId",
                table: "QualityAudits",
                column: "ChecklistTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_QualityAudits_ClosedById",
                table: "QualityAudits",
                column: "ClosedById");

            migrationBuilder.CreateIndex(
                name: "IX_QualityAudits_CreatedById",
                table: "QualityAudits",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_QualityAudits_LastDeletedById",
                table: "QualityAudits",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_QualityAudits_LastUpdatedById",
                table: "QualityAudits",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_QualityAudits_LeadAuditorId",
                table: "QualityAudits",
                column: "LeadAuditorId");

            migrationBuilder.CreateIndex(
                name: "IX_QualityAudits_MaterialId",
                table: "QualityAudits",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_QualityAudits_ProductId",
                table: "QualityAudits",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_QualityAudits_ProductionOrderId",
                table: "QualityAudits",
                column: "ProductionOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_QualityAudits_SupplierId",
                table: "QualityAudits",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_QualityAudits_VerifiedById",
                table: "QualityAudits",
                column: "VerifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_QualityAuditTeamMembers_CreatedById",
                table: "QualityAuditTeamMembers",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_QualityAuditTeamMembers_LastDeletedById",
                table: "QualityAuditTeamMembers",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_QualityAuditTeamMembers_LastUpdatedById",
                table: "QualityAuditTeamMembers",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_QualityAuditTeamMembers_QualityAuditId",
                table: "QualityAuditTeamMembers",
                column: "QualityAuditId");

            migrationBuilder.CreateIndex(
                name: "IX_QualityAuditTeamMembers_UserId",
                table: "QualityAuditTeamMembers",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditCorrectiveActions");

            migrationBuilder.DropTable(
                name: "QualityAuditTeamMembers");

            migrationBuilder.DropTable(
                name: "AuditFindings");

            migrationBuilder.DropTable(
                name: "AuditChecklistResponses");

            migrationBuilder.DropTable(
                name: "AuditChecklistTemplateItems");

            migrationBuilder.DropTable(
                name: "QualityAudits");

            migrationBuilder.DropTable(
                name: "AuditChecklistTemplates");
        }
    }
}
