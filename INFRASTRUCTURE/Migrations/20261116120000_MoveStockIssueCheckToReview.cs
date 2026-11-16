using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace INFRASTRUCTURE.Migrations;

/// <summary>
/// The warehouse check of issued stock moves from its approval workflow onto document review.
/// <list type="bullet">
/// <item>Everyone named in any stage of the "StockRequisitionIssue" workflow becomes a reviewer of stock
/// issues (one level: any one of them may check).</item>
/// <item>The roles named there, and the roles of the users named there, receive the issue check and return
/// keys for both kinds and the Pending reviews page, so the same people can still check. Being named as a
/// reviewer is still required as well as the key.</item>
/// <item>Issues still waiting in that workflow get an open review. Their old stages stay as history.</item>
/// </list>
/// </summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20261116120000_MoveStockIssueCheckToReview")]
public sealed class MoveStockIssueCheckToReview : Migration
{
    private const string Stages = """
        SELECT s."RoleId", s."UserId" FROM "ApprovalStages" s
        JOIN "Approvals" a ON a."Id" = s."ApprovalId"
        WHERE a."ItemType" = 'StockRequisitionIssue' AND a."DeletedAt" IS NULL
        """;

    private const string Keys = """
        ('CanCheckRawMaterialIssue'), ('CanCheckPackagingMaterialIssue'), ('CanReturnRawMaterialIssue'),
        ('CanReturnPackagingMaterialIssue'), ('CanViewPendingReviews')
        """;

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql($$"""
            INSERT INTO "ReviewConfigurations" ("Id", "ItemType", "CreatedAt")
            SELECT gen_random_uuid(), 'StockRequisitionIssue', now()
            WHERE EXISTS ({{Stages}})
                AND NOT EXISTS (SELECT 1 FROM "ReviewConfigurations" c WHERE c."ItemType" = 'StockRequisitionIssue' AND c."DeletedAt" IS NULL);

            INSERT INTO "ReviewConfigurationAssignees" ("Id", "ReviewConfigurationId", "RoleId", "UserId")
            SELECT gen_random_uuid(), c."Id", named."RoleId", named."UserId"
            FROM (SELECT DISTINCT "RoleId", "UserId" FROM ({{Stages}}) stages
                  WHERE ("RoleId" IS NULL) <> ("UserId" IS NULL)) named
            JOIN "ReviewConfigurations" c ON c."ItemType" = 'StockRequisitionIssue' AND c."DeletedAt" IS NULL
            WHERE NOT EXISTS (SELECT 1 FROM "ReviewConfigurationAssignees" e WHERE e."ReviewConfigurationId" = c."Id"
                AND e."RoleId" IS NOT DISTINCT FROM named."RoleId" AND e."UserId" IS NOT DISTINCT FROM named."UserId");

            WITH checker_roles AS (
                SELECT "RoleId" AS role_id FROM ({{Stages}}) stages WHERE "RoleId" IS NOT NULL
                UNION SELECT ur."RoleId" FROM ({{Stages}}) stages JOIN "userroles" ur ON ur."UserId" = stages."UserId"
            ), keys(key) AS (VALUES {{Keys}})
            INSERT INTO "roleclaims" ("RoleId", "ClaimType", "ClaimValue")
            SELECT DISTINCT r.role_id, 'permission', keys.key FROM checker_roles r CROSS JOIN keys
            WHERE NOT EXISTS (SELECT 1 FROM "roleclaims" e WHERE e."RoleId" = r.role_id AND e."ClaimType" = 'permission' AND e."ClaimValue" = keys.key);

            WITH keys(key) AS (VALUES {{Keys}})
            INSERT INTO "PermissionTypes" ("Id", "Key", "RoleClaimId", "Type")
            SELECT gen_random_uuid(), claim."ClaimValue", claim."Id", 'Access'
            FROM "roleclaims" claim JOIN keys ON claim."ClaimValue" = keys.key AND claim."ClaimType" = 'permission'
            WHERE NOT EXISTS (SELECT 1 FROM "PermissionTypes" e WHERE e."RoleClaimId" = claim."Id" AND e."Key" = claim."ClaimValue");

            INSERT INTO "DocumentReviews" ("Id", "ModelType", "ConfigurationType", "ModelId", "Submission", "Status",
                "PreparedById", "SubmittedAt", "Version")
            SELECT gen_random_uuid(), 'StockRequisitionIssue', 'StockRequisitionIssue', i."Id", 1, 0, i."IssuedById", i."IssuedAt", 1
            FROM "StockRequisitionIssues" i
            WHERE i."DeletedAt" IS NULL AND NOT i."Approved" AND NOT i."Rejected"
                AND EXISTS (SELECT 1 FROM "StockRequisitionIssueApprovals" s WHERE s."StockRequisitionIssueId" = i."Id" AND s."Status" = 0)
                AND NOT EXISTS (SELECT 1 FROM "DocumentReviews" r WHERE r."ModelType" = 'StockRequisitionIssue' AND r."ModelId" = i."Id");

            INSERT INTO "DocumentReviewEvents" ("Id", "DocumentReviewId", "Action", "ActorId", "Comments", "At")
            SELECT gen_random_uuid(), r."Id", 0, r."PreparedById", 'Awaiting its check when the issue check moved onto document review.', r."SubmittedAt"
            FROM "DocumentReviews" r
            WHERE r."ModelType" = 'StockRequisitionIssue'
                AND NOT EXISTS (SELECT 1 FROM "DocumentReviewEvents" e WHERE e."DocumentReviewId" = r."Id");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        DELETE FROM "DocumentReviewEvents" WHERE "Comments" = 'Awaiting its check when the issue check moved onto document review.';
        DELETE FROM "DocumentReviews" r WHERE r."ModelType" = 'StockRequisitionIssue' AND r."Status" = 0
            AND NOT EXISTS (SELECT 1 FROM "DocumentReviewEvents" e WHERE e."DocumentReviewId" = r."Id");
        """);
}
