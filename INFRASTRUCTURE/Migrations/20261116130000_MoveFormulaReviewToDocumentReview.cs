using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace INFRASTRUCTURE.Migrations;

/// <summary>
/// QC formula revision review moves onto document review.
/// <list type="bullet">
/// <item>Whoever the "FormulaRevision" workflow's first (review) stage names becomes the reviewer of formula
/// revisions. Its second stage still names the approver.</item>
/// <item>Roles that may review formula revisions may now return them to draft, and see Pending reviews,
/// where revisions waiting for review are listed (no longer in the approvals feed).</item>
/// <item>Revisions in review get their review: awaiting review, or confirmed by whoever already recorded it.</item>
/// </list>
/// </summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20261116130000_MoveFormulaReviewToDocumentReview")]
public sealed class MoveFormulaReviewToDocumentReview : Migration
{
    private const string Pairs = """
        ('CanReviewFormulaRevision', 'CanReturnFormulaRevision'),
        ('CanReviewFormulaRevision', 'CanViewPendingReviews')
        """;

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql($$"""
            WITH first_stage AS (
                SELECT s."RoleId", s."UserId" FROM "ApprovalStages" s JOIN "Approvals" a ON a."Id" = s."ApprovalId"
                WHERE a."ItemType" = 'FormulaRevision' AND a."DeletedAt" IS NULL
                ORDER BY s."Order" LIMIT 1)
            INSERT INTO "ReviewConfigurations" ("Id", "ItemType", "CreatedAt")
            SELECT gen_random_uuid(), 'FormulaRevision', now()
            WHERE EXISTS (SELECT 1 FROM first_stage WHERE ("RoleId" IS NULL) <> ("UserId" IS NULL))
                AND NOT EXISTS (SELECT 1 FROM "ReviewConfigurations" c WHERE c."ItemType" = 'FormulaRevision' AND c."DeletedAt" IS NULL);

            WITH first_stage AS (
                SELECT s."RoleId", s."UserId" FROM "ApprovalStages" s JOIN "Approvals" a ON a."Id" = s."ApprovalId"
                WHERE a."ItemType" = 'FormulaRevision' AND a."DeletedAt" IS NULL
                ORDER BY s."Order" LIMIT 1)
            INSERT INTO "ReviewConfigurationAssignees" ("Id", "ReviewConfigurationId", "RoleId", "UserId")
            SELECT gen_random_uuid(), c."Id", f."RoleId", f."UserId"
            FROM first_stage f JOIN "ReviewConfigurations" c ON c."ItemType" = 'FormulaRevision' AND c."DeletedAt" IS NULL
            WHERE (f."RoleId" IS NULL) <> (f."UserId" IS NULL)
                AND NOT EXISTS (SELECT 1 FROM "ReviewConfigurationAssignees" e WHERE e."ReviewConfigurationId" = c."Id");

            WITH keys(old_key, new_key) AS (VALUES {{Pairs}})
            INSERT INTO "roleclaims" ("RoleId", "ClaimType", "ClaimValue")
            SELECT DISTINCT source."RoleId", 'permission', keys.new_key
            FROM keys JOIN "roleclaims" source ON source."ClaimType" = 'permission' AND source."ClaimValue" = keys.old_key
            WHERE NOT EXISTS (SELECT 1 FROM "roleclaims" e WHERE e."RoleId" = source."RoleId"
                AND e."ClaimType" = 'permission' AND e."ClaimValue" = keys.new_key);

            WITH keys(old_key, new_key) AS (VALUES {{Pairs}})
            INSERT INTO "PermissionTypes" ("Id", "Key", "RoleClaimId", "Type")
            SELECT DISTINCT ON (target."Id", granted."Type") gen_random_uuid(), keys.new_key, target."Id", granted."Type"
            FROM keys JOIN "roleclaims" source ON source."ClaimType" = 'permission' AND source."ClaimValue" = keys.old_key
            JOIN "PermissionTypes" granted ON granted."RoleClaimId" = source."Id" AND granted."Key" = keys.old_key
            JOIN "roleclaims" target ON target."RoleId" = source."RoleId" AND target."ClaimType" = 'permission' AND target."ClaimValue" = keys.new_key
            WHERE NOT EXISTS (SELECT 1 FROM "PermissionTypes" e WHERE e."RoleClaimId" = target."Id"
                AND e."Key" = keys.new_key AND e."Type" = granted."Type");

            INSERT INTO "DocumentReviews" ("Id", "ModelType", "ConfigurationType", "ModelId", "Submission", "Status",
                "PreparedById", "SubmittedAt", "ReviewedById", "ReviewedAt", "ApprovalsStartedAt", "Version")
            SELECT gen_random_uuid(), 'FormulaRevision', 'FormulaRevision', r."Id", 1,
                CASE WHEN r."ReviewedById" IS NULL THEN 0 ELSE 2 END, r."CreatedById", COALESCE(r."UpdatedAt", r."CreatedAt"),
                r."ReviewedById", r."ReviewedAt", r."ReviewedAt", 1
            FROM "FormulaRevisions" r
            WHERE r."Status" = 1
                AND NOT EXISTS (SELECT 1 FROM "DocumentReviews" d WHERE d."ModelType" = 'FormulaRevision' AND d."ModelId" = r."Id");

            INSERT INTO "DocumentReviewEvents" ("Id", "DocumentReviewId", "Action", "ActorId", "Comments", "At")
            SELECT gen_random_uuid(), d."Id", CASE WHEN d."Status" = 2 THEN 4 ELSE 0 END,
                CASE WHEN d."Status" = 2 THEN d."ReviewedById" ELSE d."PreparedById" END,
                'In review when formula review moved onto document review.', COALESCE(d."ReviewedAt", d."SubmittedAt")
            FROM "DocumentReviews" d
            WHERE d."ModelType" = 'FormulaRevision'
                AND NOT EXISTS (SELECT 1 FROM "DocumentReviewEvents" e WHERE e."DocumentReviewId" = d."Id");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql($$"""
        DELETE FROM "DocumentReviewEvents" WHERE "Comments" = 'In review when formula review moved onto document review.';
        DELETE FROM "DocumentReviews" d WHERE d."ModelType" = 'FormulaRevision'
            AND NOT EXISTS (SELECT 1 FROM "DocumentReviewEvents" e WHERE e."DocumentReviewId" = d."Id");
        DELETE FROM "PermissionTypes" WHERE "Key" = 'CanReturnFormulaRevision';
        DELETE FROM "roleclaims" WHERE "ClaimType" = 'permission' AND "ClaimValue" = 'CanReturnFormulaRevision';
        """);
}
