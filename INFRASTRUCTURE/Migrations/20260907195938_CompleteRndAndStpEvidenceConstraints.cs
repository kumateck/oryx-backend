using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class CompleteRndAndStpEvidenceConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM "StpDocumentVersions"
                        WHERE "StorageKey" IS NULL OR "FileName" IS NULL OR "Sha256" IS NULL
                    ) OR EXISTS (
                        SELECT 1 FROM "StpDocumentSignatures" WHERE "Meaning" IS NULL
                    ) OR EXISTS (
                        SELECT 1 FROM "StpDocuments" WHERE "OwnerType" IS NULL
                    ) THEN
                        RAISE EXCEPTION 'STP evidence contains null required fields; reconcile before migration';
                    END IF;
                END $$;
                """
            );

            migrationBuilder.AlterColumn<string>(
                name: "StorageKey",
                table: "StpDocumentVersions",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Sha256",
                table: "StpDocumentVersions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FileName",
                table: "StpDocumentVersions",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Meaning",
                table: "StpDocumentSignatures",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "OwnerType",
                table: "StpDocuments",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RndTrialBatchId",
                table: "Requisitions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RndFormulations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RndProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_RndFormulations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RndFormulations_RndProjects_RndProjectId",
                        column: x => x.RndProjectId,
                        principalTable: "RndProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RndFormulations_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RndFormulations_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RndFormulations_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RndFormulationItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RndFormulationId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialTypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Grade = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CasNumber = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    IsSubstitutable = table.Column<bool>(type: "boolean", nullable: false),
                    BaseQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    BaseUoMId = table.Column<Guid>(type: "uuid", nullable: true),
                    PrescribedQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    Percentage = table.Column<decimal>(type: "numeric", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RndFormulationItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RndFormulationItems_MaterialTypes_MaterialTypeId",
                        column: x => x.MaterialTypeId,
                        principalTable: "MaterialTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RndFormulationItems_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RndFormulationItems_RndFormulations_RndFormulationId",
                        column: x => x.RndFormulationId,
                        principalTable: "RndFormulations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RndFormulationItems_UnitOfMeasures_BaseUoMId",
                        column: x => x.BaseUoMId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RndFormulationItems_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RndFormulationItems_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RndFormulationItems_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RndTrialBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RndProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    RndFormulationId = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ScaleType = table.Column<int>(type: "integer", nullable: false),
                    BatchSize = table.Column<decimal>(type: "numeric", nullable: false),
                    BatchSizeUoMId = table.Column<Guid>(type: "uuid", nullable: true),
                    ManufacturingDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PerformedById = table.Column<Guid>(type: "uuid", nullable: true),
                    ProtocolFormId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Observations = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RndTrialBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RndTrialBatches_Forms_ProtocolFormId",
                        column: x => x.ProtocolFormId,
                        principalTable: "Forms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RndTrialBatches_RndFormulations_RndFormulationId",
                        column: x => x.RndFormulationId,
                        principalTable: "RndFormulations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RndTrialBatches_RndProjects_RndProjectId",
                        column: x => x.RndProjectId,
                        principalTable: "RndProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RndTrialBatches_UnitOfMeasures_BatchSizeUoMId",
                        column: x => x.BatchSizeUoMId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RndTrialBatches_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RndTrialBatches_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RndTrialBatches_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RndTrialBatches_users_PerformedById",
                        column: x => x.PerformedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RndFormulationItemSubstitutes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RndFormulationItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubstituteMaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RndFormulationItemSubstitutes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RndFormulationItemSubstitutes_Materials_SubstituteMaterialId",
                        column: x => x.SubstituteMaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RndFormulationItemSubstitutes_RndFormulationItems_RndFormul~",
                        column: x => x.RndFormulationItemId,
                        principalTable: "RndFormulationItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RndFormulationItemSubstitutes_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RndFormulationItemSubstitutes_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RndFormulationItemSubstitutes_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_StpDocumentVersions_Size",
                table: "StpDocumentVersions",
                sql: "\"Size\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_StpDocumentVersions_VersionNumber",
                table: "StpDocumentVersions",
                sql: "\"VersionNumber\" > 0");

            migrationBuilder.CreateIndex(
                name: "IX_Requisitions_RndTrialBatchId",
                table: "Requisitions",
                column: "RndTrialBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_RndFormulationItems_BaseUoMId",
                table: "RndFormulationItems",
                column: "BaseUoMId");

            migrationBuilder.CreateIndex(
                name: "IX_RndFormulationItems_CreatedById",
                table: "RndFormulationItems",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RndFormulationItems_LastDeletedById",
                table: "RndFormulationItems",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_RndFormulationItems_LastUpdatedById",
                table: "RndFormulationItems",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RndFormulationItems_MaterialId",
                table: "RndFormulationItems",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_RndFormulationItems_MaterialTypeId",
                table: "RndFormulationItems",
                column: "MaterialTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_RndFormulationItems_RndFormulationId",
                table: "RndFormulationItems",
                column: "RndFormulationId");

            migrationBuilder.CreateIndex(
                name: "IX_RndFormulationItemSubstitutes_CreatedById",
                table: "RndFormulationItemSubstitutes",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RndFormulationItemSubstitutes_LastDeletedById",
                table: "RndFormulationItemSubstitutes",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_RndFormulationItemSubstitutes_LastUpdatedById",
                table: "RndFormulationItemSubstitutes",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RndFormulationItemSubstitutes_RndFormulationItemId",
                table: "RndFormulationItemSubstitutes",
                column: "RndFormulationItemId");

            migrationBuilder.CreateIndex(
                name: "IX_RndFormulationItemSubstitutes_SubstituteMaterialId",
                table: "RndFormulationItemSubstitutes",
                column: "SubstituteMaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_RndFormulations_CreatedById",
                table: "RndFormulations",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RndFormulations_LastDeletedById",
                table: "RndFormulations",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_RndFormulations_LastUpdatedById",
                table: "RndFormulations",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RndFormulations_RndProjectId",
                table: "RndFormulations",
                column: "RndProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_RndTrialBatches_BatchSizeUoMId",
                table: "RndTrialBatches",
                column: "BatchSizeUoMId");

            migrationBuilder.CreateIndex(
                name: "IX_RndTrialBatches_CreatedById",
                table: "RndTrialBatches",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RndTrialBatches_LastDeletedById",
                table: "RndTrialBatches",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_RndTrialBatches_LastUpdatedById",
                table: "RndTrialBatches",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RndTrialBatches_PerformedById",
                table: "RndTrialBatches",
                column: "PerformedById");

            migrationBuilder.CreateIndex(
                name: "IX_RndTrialBatches_ProtocolFormId",
                table: "RndTrialBatches",
                column: "ProtocolFormId");

            migrationBuilder.CreateIndex(
                name: "IX_RndTrialBatches_RndFormulationId",
                table: "RndTrialBatches",
                column: "RndFormulationId");

            migrationBuilder.CreateIndex(
                name: "IX_RndTrialBatches_RndProjectId",
                table: "RndTrialBatches",
                column: "RndProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_Requisitions_RndTrialBatches_RndTrialBatchId",
                table: "Requisitions",
                column: "RndTrialBatchId",
                principalTable: "RndTrialBatches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Requisitions_RndTrialBatches_RndTrialBatchId",
                table: "Requisitions");

            migrationBuilder.DropTable(
                name: "RndFormulationItemSubstitutes");

            migrationBuilder.DropTable(
                name: "RndTrialBatches");

            migrationBuilder.DropTable(
                name: "RndFormulationItems");

            migrationBuilder.DropTable(
                name: "RndFormulations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_StpDocumentVersions_Size",
                table: "StpDocumentVersions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_StpDocumentVersions_VersionNumber",
                table: "StpDocumentVersions");

            migrationBuilder.DropIndex(
                name: "IX_Requisitions_RndTrialBatchId",
                table: "Requisitions");

            migrationBuilder.DropColumn(
                name: "RndTrialBatchId",
                table: "Requisitions");

            migrationBuilder.AlterColumn<string>(
                name: "StorageKey",
                table: "StpDocumentVersions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512);

            migrationBuilder.AlterColumn<string>(
                name: "Sha256",
                table: "StpDocumentVersions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "FileName",
                table: "StpDocumentVersions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "Meaning",
                table: "StpDocumentSignatures",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "OwnerType",
                table: "StpDocuments",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);
        }
    }
}
