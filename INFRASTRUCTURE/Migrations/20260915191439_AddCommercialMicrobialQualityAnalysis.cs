using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddCommercialMicrobialQualityAnalysis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RoutineCertificates_RoutineSamples_RoutineSampleId",
                table: "RoutineCertificates");

            migrationBuilder.DropIndex(
                name: "IX_RoutineCertificates_RoutineSampleId",
                table: "RoutineCertificates");

            migrationBuilder.AlterColumn<Guid>(
                name: "RoutineSampleId",
                table: "RoutineCertificates",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "RoutineExecutionId",
                table: "RoutineCertificates",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "RoutineCertificates" AS certificate
                SET "RoutineExecutionId" = sample."RoutineExecutionId"
                FROM "RoutineSamples" AS sample
                WHERE certificate."RoutineSampleId" = sample."Id";
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "RoutineExecutionId",
                table: "RoutineCertificates",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MaterialSamplingId",
                table: "Responses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AnalysisType",
                table: "ProductAnalyticalRawData",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "ChemicalArdId",
                table: "MaterialSamplings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MicrobialArdId",
                table: "MaterialSamplings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MicrobialRequired",
                table: "MaterialSamplings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "AnalysisType",
                table: "MaterialAnalyticalRawData",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "MaterialSamplingId",
                table: "FormAssignees",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ChemicalArdId",
                table: "AnalyticalTestRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MicrobialArdId",
                table: "AnalyticalTestRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MicrobialRequired",
                table: "AnalyticalTestRequests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "CommercialCertificates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Target = table.Column<int>(type: "integer", nullable: false),
                    MaterialSamplingId = table.Column<Guid>(type: "uuid", nullable: true),
                    AnalyticalTestRequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    CertificateCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Combined = table.Column<bool>(type: "boolean", nullable: false),
                    RowsJson = table.Column<string>(type: "text", maxLength: 100000000, nullable: true),
                    IssuedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IssuedById = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommercialCertificates", x => x.Id);
                    table.CheckConstraint("CK_CommercialCertificates_Target", "(\"Target\" = 0 AND \"MaterialSamplingId\" IS NOT NULL AND \"AnalyticalTestRequestId\" IS NULL) OR (\"Target\" = 1 AND \"MaterialSamplingId\" IS NULL AND \"AnalyticalTestRequestId\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_CommercialCertificates_AnalyticalTestRequests_AnalyticalTes~",
                        column: x => x.AnalyticalTestRequestId,
                        principalTable: "AnalyticalTestRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommercialCertificates_MaterialSamplings_MaterialSamplingId",
                        column: x => x.MaterialSamplingId,
                        principalTable: "MaterialSamplings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommercialCertificates_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CommercialCertificates_users_IssuedById",
                        column: x => x.IssuedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CommercialCertificates_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CommercialCertificates_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CommercialCoaItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialArdId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductArdId = table.Column<Guid>(type: "uuid", nullable: true),
                    FormFieldId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayLabel = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    GroupName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    SpecificationText = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Unit = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Reference = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommercialCoaItems", x => x.Id);
                    table.CheckConstraint("CK_CommercialCoaItems_Ard", "(\"MaterialArdId\" IS NOT NULL AND \"ProductArdId\" IS NULL) OR (\"MaterialArdId\" IS NULL AND \"ProductArdId\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_CommercialCoaItems_FormFields_FormFieldId",
                        column: x => x.FormFieldId,
                        principalTable: "FormFields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommercialCoaItems_MaterialAnalyticalRawData_MaterialArdId",
                        column: x => x.MaterialArdId,
                        principalTable: "MaterialAnalyticalRawData",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CommercialCoaItems_ProductAnalyticalRawData_ProductArdId",
                        column: x => x.ProductArdId,
                        principalTable: "ProductAnalyticalRawData",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CommercialCoaItems_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CommercialCoaItems_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CommercialCoaItems_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.Sql("""
                INSERT INTO "CommercialCoaItems"
                    ("Id", "MaterialArdId", "FormFieldId", "DisplayLabel",
                     "GroupName", "SpecificationText", "Reference",
                     "DisplayOrder", "CreatedAt")
                SELECT md5('material-' || ard."Id"::text || field."Id"::text)::uuid,
                       ard."Id", field."Id",
                       COALESCE(NULLIF(question."Label", ''), section."Name", 'Test'),
                       section."GroupName",
                       COALESCE(NULLIF(section."Description", ''),
                                NULLIF(field."Description", ''),
                                'Configured specification'),
                       question."Reference",
                       (row_number() OVER (
                           PARTITION BY ard."Id"
                           ORDER BY section."Order", field."Rank") - 1)::integer,
                       CURRENT_TIMESTAMP
                FROM "MaterialAnalyticalRawData" AS ard
                JOIN "FormSections" AS section ON section."FormId" = ard."FormId"
                JOIN "FormFields" AS field ON field."FormSectionId" = section."Id"
                JOIN "Questions" AS question ON question."Id" = field."QuestionId"
                WHERE ard."DeletedAt" IS NULL;

                INSERT INTO "CommercialCoaItems"
                    ("Id", "ProductArdId", "FormFieldId", "DisplayLabel",
                     "GroupName", "SpecificationText", "Reference",
                     "DisplayOrder", "CreatedAt")
                SELECT md5('product-' || ard."Id"::text || field."Id"::text)::uuid,
                       ard."Id", field."Id",
                       COALESCE(NULLIF(question."Label", ''), section."Name", 'Test'),
                       section."GroupName",
                       COALESCE(NULLIF(section."Description", ''),
                                NULLIF(field."Description", ''),
                                'Configured specification'),
                       question."Reference",
                       (row_number() OVER (
                           PARTITION BY ard."Id"
                           ORDER BY section."Order", field."Rank") - 1)::integer,
                       CURRENT_TIMESTAMP
                FROM "ProductAnalyticalRawData" AS ard
                JOIN "FormSections" AS section ON section."FormId" = ard."FormId"
                JOIN "FormFields" AS field ON field."FormSectionId" = section."Id"
                JOIN "Questions" AS question ON question."Id" = field."QuestionId"
                WHERE ard."DeletedAt" IS NULL;
                """);

            migrationBuilder.CreateTable(
                name: "MicrobialRequirements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Subject = table.Column<int>(type: "integer", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: true),
                    Stage = table.Column<int>(type: "integer", nullable: true),
                    Required = table.Column<bool>(type: "boolean", nullable: false),
                    Reason = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
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
                    table.PrimaryKey("PK_MicrobialRequirements", x => x.Id);
                    table.CheckConstraint("CK_MicrobialRequirements_Subject", "(\"Subject\" = 0 AND \"MaterialId\" IS NOT NULL AND \"ProductId\" IS NULL AND \"Stage\" IS NULL) OR (\"Subject\" = 1 AND \"MaterialId\" IS NULL AND \"ProductId\" IS NOT NULL AND \"Stage\" = 2)");
                    table.ForeignKey(
                        name: "FK_MicrobialRequirements_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MicrobialRequirements_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MicrobialRequirements_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MicrobialRequirements_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MicrobialRequirements_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoutineCertificates_RoutineExecutionId",
                table: "RoutineCertificates",
                column: "RoutineExecutionId",
                unique: true,
                filter: "\"RoutineSampleId\" IS NULL AND \"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RoutineCertificates_RoutineSampleId",
                table: "RoutineCertificates",
                column: "RoutineSampleId",
                unique: true,
                filter: "\"RoutineSampleId\" IS NOT NULL AND \"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Responses_MaterialSamplingId",
                table: "Responses",
                column: "MaterialSamplingId");

            migrationBuilder.CreateIndex(
                name: "IX_FormAssignees_MaterialSamplingId",
                table: "FormAssignees",
                column: "MaterialSamplingId");

            migrationBuilder.CreateIndex(
                name: "IX_CommercialCertificates_AnalyticalTestRequestId",
                table: "CommercialCertificates",
                column: "AnalyticalTestRequestId",
                unique: true,
                filter: "\"AnalyticalTestRequestId\" IS NOT NULL AND \"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CommercialCertificates_CreatedById",
                table: "CommercialCertificates",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_CommercialCertificates_IssuedById",
                table: "CommercialCertificates",
                column: "IssuedById");

            migrationBuilder.CreateIndex(
                name: "IX_CommercialCertificates_LastDeletedById",
                table: "CommercialCertificates",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_CommercialCertificates_LastUpdatedById",
                table: "CommercialCertificates",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_CommercialCertificates_MaterialSamplingId",
                table: "CommercialCertificates",
                column: "MaterialSamplingId",
                unique: true,
                filter: "\"MaterialSamplingId\" IS NOT NULL AND \"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CommercialCoaItems_CreatedById",
                table: "CommercialCoaItems",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_CommercialCoaItems_FormFieldId",
                table: "CommercialCoaItems",
                column: "FormFieldId");

            migrationBuilder.CreateIndex(
                name: "IX_CommercialCoaItems_LastDeletedById",
                table: "CommercialCoaItems",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_CommercialCoaItems_LastUpdatedById",
                table: "CommercialCoaItems",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_CommercialCoaItems_MaterialArdId_FormFieldId",
                table: "CommercialCoaItems",
                columns: new[] { "MaterialArdId", "FormFieldId" },
                unique: true,
                filter: "\"MaterialArdId\" IS NOT NULL AND \"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CommercialCoaItems_ProductArdId_FormFieldId",
                table: "CommercialCoaItems",
                columns: new[] { "ProductArdId", "FormFieldId" },
                unique: true,
                filter: "\"ProductArdId\" IS NOT NULL AND \"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MicrobialRequirements_CreatedById",
                table: "MicrobialRequirements",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_MicrobialRequirements_LastDeletedById",
                table: "MicrobialRequirements",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_MicrobialRequirements_LastUpdatedById",
                table: "MicrobialRequirements",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_MicrobialRequirements_MaterialId",
                table: "MicrobialRequirements",
                column: "MaterialId",
                unique: true,
                filter: "\"Subject\" = 0 AND \"IsVerified\" = FALSE AND \"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MicrobialRequirements_ProductId",
                table: "MicrobialRequirements",
                column: "ProductId",
                unique: true,
                filter: "\"Subject\" = 1 AND \"IsVerified\" = FALSE AND \"DeletedAt\" IS NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_FormAssignees_MaterialSamplings_MaterialSamplingId",
                table: "FormAssignees",
                column: "MaterialSamplingId",
                principalTable: "MaterialSamplings",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Responses_MaterialSamplings_MaterialSamplingId",
                table: "Responses",
                column: "MaterialSamplingId",
                principalTable: "MaterialSamplings",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RoutineCertificates_RoutineExecutions_RoutineExecutionId",
                table: "RoutineCertificates",
                column: "RoutineExecutionId",
                principalTable: "RoutineExecutions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RoutineCertificates_RoutineSamples_RoutineSampleId",
                table: "RoutineCertificates",
                column: "RoutineSampleId",
                principalTable: "RoutineSamples",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FormAssignees_MaterialSamplings_MaterialSamplingId",
                table: "FormAssignees");

            migrationBuilder.DropForeignKey(
                name: "FK_Responses_MaterialSamplings_MaterialSamplingId",
                table: "Responses");

            migrationBuilder.DropForeignKey(
                name: "FK_RoutineCertificates_RoutineExecutions_RoutineExecutionId",
                table: "RoutineCertificates");

            migrationBuilder.DropForeignKey(
                name: "FK_RoutineCertificates_RoutineSamples_RoutineSampleId",
                table: "RoutineCertificates");

            migrationBuilder.DropTable(
                name: "CommercialCertificates");

            migrationBuilder.DropTable(
                name: "CommercialCoaItems");

            migrationBuilder.DropTable(
                name: "MicrobialRequirements");

            migrationBuilder.DropIndex(
                name: "IX_RoutineCertificates_RoutineExecutionId",
                table: "RoutineCertificates");

            migrationBuilder.DropIndex(
                name: "IX_RoutineCertificates_RoutineSampleId",
                table: "RoutineCertificates");

            migrationBuilder.DropIndex(
                name: "IX_Responses_MaterialSamplingId",
                table: "Responses");

            migrationBuilder.DropIndex(
                name: "IX_FormAssignees_MaterialSamplingId",
                table: "FormAssignees");

            migrationBuilder.DropColumn(
                name: "RoutineExecutionId",
                table: "RoutineCertificates");

            migrationBuilder.DropColumn(
                name: "MaterialSamplingId",
                table: "Responses");

            migrationBuilder.DropColumn(
                name: "AnalysisType",
                table: "ProductAnalyticalRawData");

            migrationBuilder.DropColumn(
                name: "ChemicalArdId",
                table: "MaterialSamplings");

            migrationBuilder.DropColumn(
                name: "MicrobialArdId",
                table: "MaterialSamplings");

            migrationBuilder.DropColumn(
                name: "MicrobialRequired",
                table: "MaterialSamplings");

            migrationBuilder.DropColumn(
                name: "AnalysisType",
                table: "MaterialAnalyticalRawData");

            migrationBuilder.DropColumn(
                name: "MaterialSamplingId",
                table: "FormAssignees");

            migrationBuilder.DropColumn(
                name: "ChemicalArdId",
                table: "AnalyticalTestRequests");

            migrationBuilder.DropColumn(
                name: "MicrobialArdId",
                table: "AnalyticalTestRequests");

            migrationBuilder.DropColumn(
                name: "MicrobialRequired",
                table: "AnalyticalTestRequests");

            migrationBuilder.AlterColumn<Guid>(
                name: "RoutineSampleId",
                table: "RoutineCertificates",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoutineCertificates_RoutineSampleId",
                table: "RoutineCertificates",
                column: "RoutineSampleId",
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_RoutineCertificates_RoutineSamples_RoutineSampleId",
                table: "RoutineCertificates",
                column: "RoutineSampleId",
                principalTable: "RoutineSamples",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
