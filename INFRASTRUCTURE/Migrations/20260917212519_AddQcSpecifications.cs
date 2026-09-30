using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddQcSpecifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QcSamplingPointGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QcSamplingPointGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QcSamplingPointGroups_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QcSamplingPointGroups_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QcSamplingPointGroups_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "QcSpecifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Approved = table.Column<bool>(type: "boolean", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    AppliesTo = table.Column<int>(type: "integer", nullable: false),
                    Stage = table.Column<int>(type: "integer", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    SupersedesId = table.Column<Guid>(type: "uuid", nullable: true),
                    EffectiveDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RetestPolicy = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QcSpecifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QcSpecifications_QcSpecifications_SupersedesId",
                        column: x => x.SupersedesId,
                        principalTable: "QcSpecifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QcSpecifications_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QcSpecifications_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QcSpecifications_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "QcSpecificationCharacteristics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SpecificationId = table.Column<Guid>(type: "uuid", nullable: false),
                    TestName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Analyte = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AcceptanceCriteria = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    AlertLimit = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ActionLimit = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SamplingPointGroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceWorksheetTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceFieldKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IncludeOnCoa = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    GroupName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QcSpecificationCharacteristics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QcSpecificationCharacteristics_QcSamplingPointGroups_Sampli~",
                        column: x => x.SamplingPointGroupId,
                        principalTable: "QcSamplingPointGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QcSpecificationCharacteristics_QcSpecifications_Specificati~",
                        column: x => x.SpecificationId,
                        principalTable: "QcSpecifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QcSpecificationCharacteristics_QcWorksheetTemplates_SourceW~",
                        column: x => x.SourceWorksheetTemplateId,
                        principalTable: "QcWorksheetTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QcSpecificationCharacteristics_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QcSpecificationCharacteristics_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QcSpecificationCharacteristics_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "QcSpecificationWorksheetLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SpecificationId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorksheetTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    AnalysisType = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QcSpecificationWorksheetLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QcSpecificationWorksheetLinks_QcSpecifications_Specificatio~",
                        column: x => x.SpecificationId,
                        principalTable: "QcSpecifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QcSpecificationWorksheetLinks_QcWorksheetTemplates_Workshee~",
                        column: x => x.WorksheetTemplateId,
                        principalTable: "QcWorksheetTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QcSpecificationWorksheetLinks_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QcSpecificationWorksheetLinks_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QcSpecificationWorksheetLinks_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_QcSamplingPointGroups_CreatedById",
                table: "QcSamplingPointGroups",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcSamplingPointGroups_LastDeletedById",
                table: "QcSamplingPointGroups",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcSamplingPointGroups_LastUpdatedById",
                table: "QcSamplingPointGroups",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcSamplingPointGroups_Name",
                table: "QcSamplingPointGroups",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_QcSpecificationCharacteristics_CreatedById",
                table: "QcSpecificationCharacteristics",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcSpecificationCharacteristics_LastDeletedById",
                table: "QcSpecificationCharacteristics",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcSpecificationCharacteristics_LastUpdatedById",
                table: "QcSpecificationCharacteristics",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcSpecificationCharacteristics_SamplingPointGroupId",
                table: "QcSpecificationCharacteristics",
                column: "SamplingPointGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_QcSpecificationCharacteristics_SourceWorksheetTemplateId_So~",
                table: "QcSpecificationCharacteristics",
                columns: new[] { "SourceWorksheetTemplateId", "SourceFieldKey" });

            migrationBuilder.CreateIndex(
                name: "IX_QcSpecificationCharacteristics_SpecificationId_DisplayOrder",
                table: "QcSpecificationCharacteristics",
                columns: new[] { "SpecificationId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_QcSpecifications_AppliesTo_Stage",
                table: "QcSpecifications",
                columns: new[] { "AppliesTo", "Stage" });

            migrationBuilder.CreateIndex(
                name: "IX_QcSpecifications_Code",
                table: "QcSpecifications",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_QcSpecifications_CreatedById",
                table: "QcSpecifications",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcSpecifications_LastDeletedById",
                table: "QcSpecifications",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcSpecifications_LastUpdatedById",
                table: "QcSpecifications",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcSpecifications_Status",
                table: "QcSpecifications",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_QcSpecifications_SupersedesId",
                table: "QcSpecifications",
                column: "SupersedesId");

            migrationBuilder.CreateIndex(
                name: "IX_QcSpecificationWorksheetLinks_CreatedById",
                table: "QcSpecificationWorksheetLinks",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcSpecificationWorksheetLinks_LastDeletedById",
                table: "QcSpecificationWorksheetLinks",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcSpecificationWorksheetLinks_LastUpdatedById",
                table: "QcSpecificationWorksheetLinks",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_QcSpecificationWorksheetLinks_SpecificationId_AnalysisType",
                table: "QcSpecificationWorksheetLinks",
                columns: new[] { "SpecificationId", "AnalysisType" });

            migrationBuilder.CreateIndex(
                name: "IX_QcSpecificationWorksheetLinks_WorksheetTemplateId",
                table: "QcSpecificationWorksheetLinks",
                column: "WorksheetTemplateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QcSpecificationCharacteristics");

            migrationBuilder.DropTable(
                name: "QcSpecificationWorksheetLinks");

            migrationBuilder.DropTable(
                name: "QcSamplingPointGroups");

            migrationBuilder.DropTable(
                name: "QcSpecifications");
        }
    }
}
