using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddFormulaV1Foundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FormRevisionId",
                table: "Responses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FormulaSubmissionSetId",
                table: "ResponseApprovals",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FormFieldRevisionId",
                table: "FormResponses",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FormRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FormId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ApprovedById = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormRevisions", x => x.Id);
                    table.CheckConstraint("CK_FormRevision_ContentHash", "\"ContentHash\" ~ '^[a-f0-9]{64}$'");
                    table.CheckConstraint("CK_FormRevision_Sequence", "\"Sequence\" > 0");
                    table.CheckConstraint("CK_FormRevision_Status", "\"Status\" BETWEEN 0 AND 3");
                    table.ForeignKey(
                        name: "FK_FormRevisions_Forms_FormId",
                        column: x => x.FormId,
                        principalTable: "Forms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FormRevisions_users_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FormRevisions_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FormRevisions_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FormRevisions_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FormulaDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    PresentationPreset = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormulaDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormulaDefinitions_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FormulaDefinitions_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FormulaDefinitions_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FormulaMigrationRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SourceFingerprint = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CorpusChecksum = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CodeVersion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Mode = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    InitiatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TotalCount = table.Column<int>(type: "integer", nullable: false),
                    SucceededCount = table.Column<int>(type: "integer", nullable: false),
                    FailedCount = table.Column<int>(type: "integer", nullable: false),
                    SignedReportLocation = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormulaMigrationRuns", x => x.Id);
                    table.CheckConstraint("CK_FormulaMigrationRun_Counts", "\"TotalCount\" >= 0 AND \"SucceededCount\" >= 0 AND \"FailedCount\" >= 0");
                    table.CheckConstraint("CK_FormulaMigrationRun_Mode", "\"Mode\" BETWEEN 0 AND 1");
                    table.CheckConstraint("CK_FormulaMigrationRun_Status", "\"Status\" BETWEEN 0 AND 3");
                    table.ForeignKey(
                        name: "FK_FormulaMigrationRuns_users_InitiatedById",
                        column: x => x.InitiatedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LegacyFormulaArtifacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionOptionId = table.Column<Guid>(type: "uuid", nullable: true),
                    OriginalPayload = table.Column<string>(type: "jsonb", nullable: false),
                    SourceHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SourcePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SourceWasDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    SourceCreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SourceUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SourceDeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DatabaseFingerprint = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    MigrationReleaseId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CapturedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegacyFormulaArtifacts", x => x.Id);
                    table.CheckConstraint("CK_LegacyFormulaArtifact_SourceHash", "\"SourceHash\" ~ '^[a-f0-9]{64}$'");
                    table.ForeignKey(
                        name: "FK_LegacyFormulaArtifacts_QuestionOptions_QuestionOptionId",
                        column: x => x.QuestionOptionId,
                        principalTable: "QuestionOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LegacyFormulaArtifacts_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ResponseFormulaSubmissionSets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ResponseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    SetHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    InputAggregateHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ConfigurationAggregateHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SubmittedById = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResponseFormulaSubmissionSets", x => x.Id);
                    table.CheckConstraint("CK_ResponseFormulaSubmissionSet_Sequence", "\"Sequence\" > 0");
                    table.ForeignKey(
                        name: "FK_ResponseFormulaSubmissionSets_Responses_ResponseId",
                        column: x => x.ResponseId,
                        principalTable: "Responses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResponseFormulaSubmissionSets_users_SubmittedById",
                        column: x => x.SubmittedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FormFieldRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FormRevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlacementKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    FormFieldId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Required = table.Column<bool>(type: "boolean", nullable: false),
                    Rank = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000000)", maxLength: 1000000, nullable: true),
                    FieldHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormFieldRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormFieldRevisions_FormFields_FormFieldId",
                        column: x => x.FormFieldId,
                        principalTable: "FormFields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FormFieldRevisions_FormRevisions_FormRevisionId",
                        column: x => x.FormRevisionId,
                        principalTable: "FormRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FormFieldRevisions_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FormFieldRevisions_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FormFieldRevisions_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FormFieldRevisions_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FormulaRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FormulaDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    DefinitionJson = table.Column<string>(type: "jsonb", nullable: false),
                    TestCasesJson = table.Column<string>(type: "jsonb", nullable: false),
                    DefinitionHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ReleaseEvidenceHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FormulaLanguageVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    NumericPolicyVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ReviewedById = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedById = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EffectiveAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_FormulaRevisions", x => x.Id);
                    table.CheckConstraint("CK_FormulaRevision_DefinitionHash", "\"DefinitionHash\" ~ '^[a-f0-9]{64}$'");
                    table.CheckConstraint("CK_FormulaRevision_EvidenceHash", "\"ReleaseEvidenceHash\" ~ '^[a-f0-9]{64}$'");
                    table.CheckConstraint("CK_FormulaRevision_Revision", "\"Revision\" > 0");
                    table.CheckConstraint("CK_FormulaRevision_Status", "\"Status\" BETWEEN 0 AND 3");
                    table.ForeignKey(
                        name: "FK_FormulaRevisions_FormulaDefinitions_FormulaDefinitionId",
                        column: x => x.FormulaDefinitionId,
                        principalTable: "FormulaDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FormulaRevisions_users_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FormulaRevisions_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FormulaRevisions_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FormulaRevisions_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FormulaRevisions_users_ReviewedById",
                        column: x => x.ReviewedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QuestionFormulaDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    FormulaDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionFormulaDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestionFormulaDefinitions_FormulaDefinitions_FormulaDefini~",
                        column: x => x.FormulaDefinitionId,
                        principalTable: "FormulaDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuestionFormulaDefinitions_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuestionFormulaDefinitions_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QuestionFormulaDefinitions_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QuestionFormulaDefinitions_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FormulaReconciliationResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FormulaMigrationRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    ControlName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ExpectedValue = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ActualValue = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Passed = table.Column<bool>(type: "boolean", nullable: false),
                    EvidenceJson = table.Column<string>(type: "jsonb", nullable: false),
                    CheckedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormulaReconciliationResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormulaReconciliationResults_FormulaMigrationRuns_FormulaMi~",
                        column: x => x.FormulaMigrationRunId,
                        principalTable: "FormulaMigrationRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FormulaMigrationItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FormulaMigrationRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    LegacyFormulaArtifactId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PlacementKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    TargetId = table.Column<Guid>(type: "uuid", nullable: true),
                    MigrationClass = table.Column<int>(type: "integer", nullable: false),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ApprovalReference = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Error = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    BeforeHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AfterHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormulaMigrationItems", x => x.Id);
                    table.CheckConstraint("CK_FormulaMigrationItem_Class", "\"MigrationClass\" BETWEEN 0 AND 3");
                    table.CheckConstraint("CK_FormulaMigrationItem_Status", "\"Status\" BETWEEN 0 AND 3");
                    table.ForeignKey(
                        name: "FK_FormulaMigrationItems_FormulaMigrationRuns_FormulaMigration~",
                        column: x => x.FormulaMigrationRunId,
                        principalTable: "FormulaMigrationRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FormulaMigrationItems_LegacyFormulaArtifacts_LegacyFormulaA~",
                        column: x => x.LegacyFormulaArtifactId,
                        principalTable: "LegacyFormulaArtifacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LegacyKeyMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LegacyFormulaArtifactId = table.Column<Guid>(type: "uuid", nullable: false),
                    KeyKind = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LegacyPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    LegacyKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    CanonicalKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    NormalizationReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ReviewedById = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovalReference = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegacyKeyMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LegacyKeyMappings_LegacyFormulaArtifacts_LegacyFormulaArtif~",
                        column: x => x.LegacyFormulaArtifactId,
                        principalTable: "LegacyFormulaArtifacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LegacyKeyMappings_users_ReviewedById",
                        column: x => x.ReviewedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FormFieldFormulaConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FormFieldRevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    FormulaRevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    BindingsJson = table.Column<string>(type: "jsonb", nullable: false),
                    ResultTargetsJson = table.Column<string>(type: "jsonb", nullable: false),
                    DisplayPolicyJson = table.Column<string>(type: "jsonb", nullable: false),
                    MethodReference = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    ConfigurationHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormFieldFormulaConfigurations", x => x.Id);
                    table.CheckConstraint("CK_FormFieldFormulaConfiguration_Hash", "\"ConfigurationHash\" ~ '^[a-f0-9]{64}$'");
                    table.ForeignKey(
                        name: "FK_FormFieldFormulaConfigurations_FormFieldRevisions_FormField~",
                        column: x => x.FormFieldRevisionId,
                        principalTable: "FormFieldRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FormFieldFormulaConfigurations_FormulaRevisions_FormulaRevi~",
                        column: x => x.FormulaRevisionId,
                        principalTable: "FormulaRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FormFieldFormulaConfigurations_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FormFieldFormulaConfigurations_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FormFieldFormulaConfigurations_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FormulaRevisionAudits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FormulaRevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PriorStatus = table.Column<int>(type: "integer", nullable: true),
                    NewStatus = table.Column<int>(type: "integer", nullable: false),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Reason = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    DefinitionHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormulaRevisionAudits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormulaRevisionAudits_FormulaRevisions_FormulaRevisionId",
                        column: x => x.FormulaRevisionId,
                        principalTable: "FormulaRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FormulaRevisionAudits_users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ResponseFormulaSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ResponseId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlacementKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    FormulaRevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    DefinitionHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ConfigurationHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExecutableDefinitionJson = table.Column<string>(type: "jsonb", nullable: false),
                    BindingsJson = table.Column<string>(type: "jsonb", nullable: false),
                    TableShapeJson = table.Column<string>(type: "jsonb", nullable: false),
                    CalculationPolicyJson = table.Column<string>(type: "jsonb", nullable: false),
                    DisplayPolicyJson = table.Column<string>(type: "jsonb", nullable: false),
                    SupersedesSnapshotId = table.Column<Guid>(type: "uuid", nullable: true),
                    Reason = table.Column<int>(type: "integer", nullable: false),
                    ApprovalReference = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    CapturedById = table.Column<Guid>(type: "uuid", nullable: false),
                    CapturedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResponseFormulaSnapshots", x => x.Id);
                    table.CheckConstraint("CK_ResponseFormulaSnapshot_ConfigurationHash", "\"ConfigurationHash\" ~ '^[a-f0-9]{64}$'");
                    table.CheckConstraint("CK_ResponseFormulaSnapshot_DefinitionHash", "\"DefinitionHash\" ~ '^[a-f0-9]{64}$'");
                    table.CheckConstraint("CK_ResponseFormulaSnapshot_Reason", "\"Reason\" BETWEEN 0 AND 2");
                    table.CheckConstraint("CK_ResponseFormulaSnapshot_Sequence", "\"Sequence\" > 0");
                    table.ForeignKey(
                        name: "FK_ResponseFormulaSnapshots_FormulaRevisions_FormulaRevisionId",
                        column: x => x.FormulaRevisionId,
                        principalTable: "FormulaRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResponseFormulaSnapshots_ResponseFormulaSnapshots_Supersede~",
                        column: x => x.SupersedesSnapshotId,
                        principalTable: "ResponseFormulaSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResponseFormulaSnapshots_Responses_ResponseId",
                        column: x => x.ResponseId,
                        principalTable: "Responses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResponseFormulaSnapshots_users_CapturedById",
                        column: x => x.CapturedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FormulaExecutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ResponseFormulaSnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupersedesExecutionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Trigger = table.Column<int>(type: "integer", nullable: false),
                    Authority = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    EngineVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EngineBuildHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FormulaLanguageVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    NumericPolicyVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    InputHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ResultHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ResolvedInputsJson = table.Column<string>(type: "jsonb", nullable: false),
                    RawResultsJson = table.Column<string>(type: "jsonb", nullable: true),
                    RoundedResultsJson = table.Column<string>(type: "jsonb", nullable: true),
                    DisplayResultsJson = table.Column<string>(type: "jsonb", nullable: true),
                    CalculationTraceJson = table.Column<string>(type: "jsonb", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExecutedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormulaExecutions", x => x.Id);
                    table.CheckConstraint("CK_FormulaExecution_Authority", "\"Authority\" BETWEEN 0 AND 1");
                    table.CheckConstraint("CK_FormulaExecution_InputHash", "\"InputHash\" ~ '^[a-f0-9]{64}$'");
                    table.CheckConstraint("CK_FormulaExecution_ResultHash", "\"ResultHash\" IS NULL OR \"ResultHash\" ~ '^[a-f0-9]{64}$'");
                    table.CheckConstraint("CK_FormulaExecution_Status", "\"Status\" BETWEEN 0 AND 7");
                    table.CheckConstraint("CK_FormulaExecution_TraceSize", "octet_length(\"CalculationTraceJson\"::text) <= 65536");
                    table.CheckConstraint("CK_FormulaExecution_Trigger", "\"Trigger\" BETWEEN 0 AND 3");
                    table.ForeignKey(
                        name: "FK_FormulaExecutions_FormulaExecutions_SupersedesExecutionId",
                        column: x => x.SupersedesExecutionId,
                        principalTable: "FormulaExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FormulaExecutions_ResponseFormulaSnapshots_ResponseFormulaS~",
                        column: x => x.ResponseFormulaSnapshotId,
                        principalTable: "ResponseFormulaSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FormulaExecutions_users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ResponseFormulaSubmissionExecutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ResponseFormulaSubmissionSetId = table.Column<Guid>(type: "uuid", nullable: false),
                    FormulaExecutionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlacementKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Ordinal = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResponseFormulaSubmissionExecutions", x => x.Id);
                    table.CheckConstraint("CK_ResponseFormulaSubmissionExecution_Ordinal", "\"Ordinal\" >= 0");
                    table.ForeignKey(
                        name: "FK_ResponseFormulaSubmissionExecutions_FormulaExecutions_Formu~",
                        column: x => x.FormulaExecutionId,
                        principalTable: "FormulaExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResponseFormulaSubmissionExecutions_ResponseFormulaSubmissi~",
                        column: x => x.ResponseFormulaSubmissionSetId,
                        principalTable: "ResponseFormulaSubmissionSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Responses_FormRevisionId",
                table: "Responses",
                column: "FormRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_ResponseApprovals_FormulaSubmissionSetId",
                table: "ResponseApprovals",
                column: "FormulaSubmissionSetId");

            migrationBuilder.CreateIndex(
                name: "IX_FormResponses_FormFieldRevisionId",
                table: "FormResponses",
                column: "FormFieldRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_FormFieldFormulaConfigurations_ConfigurationHash",
                table: "FormFieldFormulaConfigurations",
                column: "ConfigurationHash");

            migrationBuilder.CreateIndex(
                name: "IX_FormFieldFormulaConfigurations_CreatedById",
                table: "FormFieldFormulaConfigurations",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_FormFieldFormulaConfigurations_FormFieldRevisionId",
                table: "FormFieldFormulaConfigurations",
                column: "FormFieldRevisionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FormFieldFormulaConfigurations_FormulaRevisionId",
                table: "FormFieldFormulaConfigurations",
                column: "FormulaRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_FormFieldFormulaConfigurations_LastDeletedById",
                table: "FormFieldFormulaConfigurations",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_FormFieldFormulaConfigurations_LastUpdatedById",
                table: "FormFieldFormulaConfigurations",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_FormFieldRevisions_CreatedById",
                table: "FormFieldRevisions",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_FormFieldRevisions_FormFieldId",
                table: "FormFieldRevisions",
                column: "FormFieldId");

            migrationBuilder.CreateIndex(
                name: "IX_FormFieldRevisions_FormRevisionId_PlacementKey",
                table: "FormFieldRevisions",
                columns: new[] { "FormRevisionId", "PlacementKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FormFieldRevisions_LastDeletedById",
                table: "FormFieldRevisions",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_FormFieldRevisions_LastUpdatedById",
                table: "FormFieldRevisions",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_FormFieldRevisions_QuestionId",
                table: "FormFieldRevisions",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_FormRevisions_ApprovedById",
                table: "FormRevisions",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_FormRevisions_CreatedById",
                table: "FormRevisions",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_FormRevisions_FormId_Sequence",
                table: "FormRevisions",
                columns: new[] { "FormId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FormRevisions_LastDeletedById",
                table: "FormRevisions",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_FormRevisions_LastUpdatedById",
                table: "FormRevisions",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_FormulaDefinitions_CreatedById",
                table: "FormulaDefinitions",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_FormulaDefinitions_LastDeletedById",
                table: "FormulaDefinitions",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_FormulaDefinitions_LastUpdatedById",
                table: "FormulaDefinitions",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_FormulaExecutions_ActorId",
                table: "FormulaExecutions",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_FormulaExecutions_ResponseFormulaSnapshotId_IdempotencyKey",
                table: "FormulaExecutions",
                columns: new[] { "ResponseFormulaSnapshotId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FormulaExecutions_SupersedesExecutionId",
                table: "FormulaExecutions",
                column: "SupersedesExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_FormulaMigrationItems_FormulaMigrationRunId_LegacyFormulaAr~",
                table: "FormulaMigrationItems",
                columns: new[] { "FormulaMigrationRunId", "LegacyFormulaArtifactId", "PlacementKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FormulaMigrationItems_LegacyFormulaArtifactId",
                table: "FormulaMigrationItems",
                column: "LegacyFormulaArtifactId");

            migrationBuilder.CreateIndex(
                name: "IX_FormulaMigrationRuns_InitiatedById",
                table: "FormulaMigrationRuns",
                column: "InitiatedById");

            migrationBuilder.CreateIndex(
                name: "IX_FormulaMigrationRuns_ReleaseId_SourceFingerprint_Mode",
                table: "FormulaMigrationRuns",
                columns: new[] { "ReleaseId", "SourceFingerprint", "Mode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FormulaReconciliationResults_FormulaMigrationRunId_ControlN~",
                table: "FormulaReconciliationResults",
                columns: new[] { "FormulaMigrationRunId", "ControlName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FormulaRevisionAudits_ActorId",
                table: "FormulaRevisionAudits",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_FormulaRevisionAudits_FormulaRevisionId_OccurredAt",
                table: "FormulaRevisionAudits",
                columns: new[] { "FormulaRevisionId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FormulaRevisions_ApprovedById",
                table: "FormulaRevisions",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_FormulaRevisions_CreatedById",
                table: "FormulaRevisions",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_FormulaRevisions_DefinitionHash_FormulaLanguageVersion_Nume~",
                table: "FormulaRevisions",
                columns: new[] { "DefinitionHash", "FormulaLanguageVersion", "NumericPolicyVersion" });

            migrationBuilder.CreateIndex(
                name: "IX_FormulaRevisions_FormulaDefinitionId_Revision",
                table: "FormulaRevisions",
                columns: new[] { "FormulaDefinitionId", "Revision" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FormulaRevisions_LastDeletedById",
                table: "FormulaRevisions",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_FormulaRevisions_LastUpdatedById",
                table: "FormulaRevisions",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_FormulaRevisions_ReviewedById",
                table: "FormulaRevisions",
                column: "ReviewedById");

            migrationBuilder.CreateIndex(
                name: "IX_LegacyFormulaArtifacts_DatabaseFingerprint_SourcePath_Sourc~",
                table: "LegacyFormulaArtifacts",
                columns: new[] { "DatabaseFingerprint", "SourcePath", "SourceHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LegacyFormulaArtifacts_QuestionId",
                table: "LegacyFormulaArtifacts",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_LegacyFormulaArtifacts_QuestionOptionId",
                table: "LegacyFormulaArtifacts",
                column: "QuestionOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_LegacyKeyMappings_LegacyFormulaArtifactId_KeyKind_LegacyPat~",
                table: "LegacyKeyMappings",
                columns: new[] { "LegacyFormulaArtifactId", "KeyKind", "LegacyPath", "LegacyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LegacyKeyMappings_ReviewedById",
                table: "LegacyKeyMappings",
                column: "ReviewedById");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionFormulaDefinitions_CreatedById",
                table: "QuestionFormulaDefinitions",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionFormulaDefinitions_FormulaDefinitionId",
                table: "QuestionFormulaDefinitions",
                column: "FormulaDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionFormulaDefinitions_LastDeletedById",
                table: "QuestionFormulaDefinitions",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionFormulaDefinitions_LastUpdatedById",
                table: "QuestionFormulaDefinitions",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionFormulaDefinitions_QuestionId_FormulaDefinitionId",
                table: "QuestionFormulaDefinitions",
                columns: new[] { "QuestionId", "FormulaDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResponseFormulaSnapshots_CapturedById",
                table: "ResponseFormulaSnapshots",
                column: "CapturedById");

            migrationBuilder.CreateIndex(
                name: "IX_ResponseFormulaSnapshots_FormulaRevisionId",
                table: "ResponseFormulaSnapshots",
                column: "FormulaRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_ResponseFormulaSnapshots_ResponseId_PlacementKey_Sequence",
                table: "ResponseFormulaSnapshots",
                columns: new[] { "ResponseId", "PlacementKey", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResponseFormulaSnapshots_SupersedesSnapshotId",
                table: "ResponseFormulaSnapshots",
                column: "SupersedesSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_ResponseFormulaSubmissionExecutions_FormulaExecutionId",
                table: "ResponseFormulaSubmissionExecutions",
                column: "FormulaExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_ResponseFormulaSubmissionExecutions_ResponseFormulaSubmiss~1",
                table: "ResponseFormulaSubmissionExecutions",
                columns: new[] { "ResponseFormulaSubmissionSetId", "PlacementKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResponseFormulaSubmissionExecutions_ResponseFormulaSubmissi~",
                table: "ResponseFormulaSubmissionExecutions",
                columns: new[] { "ResponseFormulaSubmissionSetId", "Ordinal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResponseFormulaSubmissionSets_ResponseId_Sequence",
                table: "ResponseFormulaSubmissionSets",
                columns: new[] { "ResponseId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResponseFormulaSubmissionSets_SubmittedById",
                table: "ResponseFormulaSubmissionSets",
                column: "SubmittedById");

            migrationBuilder.AddForeignKey(
                name: "FK_FormResponses_FormFieldRevisions_FormFieldRevisionId",
                table: "FormResponses",
                column: "FormFieldRevisionId",
                principalTable: "FormFieldRevisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ResponseApprovals_ResponseFormulaSubmissionSets_FormulaSubm~",
                table: "ResponseApprovals",
                column: "FormulaSubmissionSetId",
                principalTable: "ResponseFormulaSubmissionSets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Responses_FormRevisions_FormRevisionId",
                table: "Responses",
                column: "FormRevisionId",
                principalTable: "FormRevisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FormResponses_FormFieldRevisions_FormFieldRevisionId",
                table: "FormResponses");

            migrationBuilder.DropForeignKey(
                name: "FK_ResponseApprovals_ResponseFormulaSubmissionSets_FormulaSubm~",
                table: "ResponseApprovals");

            migrationBuilder.DropForeignKey(
                name: "FK_Responses_FormRevisions_FormRevisionId",
                table: "Responses");

            migrationBuilder.DropTable(
                name: "FormFieldFormulaConfigurations");

            migrationBuilder.DropTable(
                name: "FormulaMigrationItems");

            migrationBuilder.DropTable(
                name: "FormulaReconciliationResults");

            migrationBuilder.DropTable(
                name: "FormulaRevisionAudits");

            migrationBuilder.DropTable(
                name: "LegacyKeyMappings");

            migrationBuilder.DropTable(
                name: "QuestionFormulaDefinitions");

            migrationBuilder.DropTable(
                name: "ResponseFormulaSubmissionExecutions");

            migrationBuilder.DropTable(
                name: "FormFieldRevisions");

            migrationBuilder.DropTable(
                name: "FormulaMigrationRuns");

            migrationBuilder.DropTable(
                name: "LegacyFormulaArtifacts");

            migrationBuilder.DropTable(
                name: "FormulaExecutions");

            migrationBuilder.DropTable(
                name: "ResponseFormulaSubmissionSets");

            migrationBuilder.DropTable(
                name: "FormRevisions");

            migrationBuilder.DropTable(
                name: "ResponseFormulaSnapshots");

            migrationBuilder.DropTable(
                name: "FormulaRevisions");

            migrationBuilder.DropTable(
                name: "FormulaDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_Responses_FormRevisionId",
                table: "Responses");

            migrationBuilder.DropIndex(
                name: "IX_ResponseApprovals_FormulaSubmissionSetId",
                table: "ResponseApprovals");

            migrationBuilder.DropIndex(
                name: "IX_FormResponses_FormFieldRevisionId",
                table: "FormResponses");

            migrationBuilder.DropColumn(
                name: "FormRevisionId",
                table: "Responses");

            migrationBuilder.DropColumn(
                name: "FormulaSubmissionSetId",
                table: "ResponseApprovals");

            migrationBuilder.DropColumn(
                name: "FormFieldRevisionId",
                table: "FormResponses");
        }
    }
}
