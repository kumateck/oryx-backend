using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace INFRASTRUCTURE.Migrations;

/// <summary>
/// Document review keys, granted with the same access types to the roles that hold the matching key today:
/// approval settings managers manage review settings; purchase order checkers may return an order to its
/// preparer and see their pending reviews; purchase requisition raisers may correct a requisition in review.
/// The requisition review and return keys are granted to nobody: requisitions have no review step until an
/// administrator configures one and assigns them.
///
/// Purchase orders already awaiting their check get an open review, so their check is recorded the same way.
/// </summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20261116070000_GrantDocumentReviewKeys")]
public sealed class GrantDocumentReviewKeys : Migration
{
    private const string Pairs = """
        ('CanViewApprovals', 'CanViewDocumentReviewSettings'),
        ('CanEditApprovalWorkflow', 'CanEditDocumentReviewSettings'),
        ('CanDeleteApprovals', 'CanDeleteDocumentReviewSettings'),
        ('CanCheckForeignPurchaseOrder', 'CanReturnForeignPurchaseOrderCheck'),
        ('CanCheckLocalPurchaseOrder', 'CanReturnLocalPurchaseOrderCheck'),
        ('CanCheckForeignPurchaseOrder', 'CanViewPendingReviews'),
        ('CanCheckLocalPurchaseOrder', 'CanViewPendingReviews'),
        ('CanCreateRawMaterialRequisitions', 'CanEditRawMaterialPurchaseRequisition'),
        ('CanCreatePackageMaterialRequisitions', 'CanEditPackageMaterialPurchaseRequisition')
        """;

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql($$"""
            WITH keys(old_key, new_key) AS (VALUES {{Pairs}})
            INSERT INTO "roleclaims" ("RoleId", "ClaimType", "ClaimValue")
            SELECT DISTINCT source."RoleId", 'permission', keys.new_key
            FROM keys JOIN "roleclaims" source ON source."ClaimType" = 'permission' AND source."ClaimValue" = keys.old_key
            WHERE NOT EXISTS (
                SELECT 1 FROM "roleclaims" existing WHERE existing."RoleId" = source."RoleId"
                    AND existing."ClaimType" = 'permission' AND existing."ClaimValue" = keys.new_key);

            WITH keys(old_key, new_key) AS (VALUES {{Pairs}})
            INSERT INTO "PermissionTypes" ("Id", "Key", "RoleClaimId", "Type")
            SELECT DISTINCT ON (target."Id", granted."Type") gen_random_uuid(), keys.new_key, target."Id", granted."Type"
            FROM keys JOIN "roleclaims" source ON source."ClaimType" = 'permission' AND source."ClaimValue" = keys.old_key
            JOIN "PermissionTypes" granted ON granted."RoleClaimId" = source."Id" AND granted."Key" = keys.old_key
            JOIN "roleclaims" target ON target."RoleId" = source."RoleId" AND target."ClaimType" = 'permission'
                AND target."ClaimValue" = keys.new_key
            WHERE NOT EXISTS (
                SELECT 1 FROM "PermissionTypes" existing WHERE existing."RoleClaimId" = target."Id"
                    AND existing."Key" = keys.new_key AND existing."Type" = granted."Type");
            """);

        // Orders finalised (4) or revised (10) and still waiting for their check.
        migrationBuilder.Sql("""
            INSERT INTO "DocumentReviews" ("Id", "ModelType", "ConfigurationType", "ModelId", "Submission", "Status",
                "PreparedById", "SubmittedAt", "Version")
            SELECT gen_random_uuid(), 'PurchaseOrder', 'PurchaseOrder', po."Id", 1, 0, po."CreatedById",
                COALESCE(po."UpdatedAt", po."CreatedAt"), 1
            FROM "PurchaseOrders" po
            WHERE po."DeletedAt" IS NULL AND po."Status" IN (4, 10)
                AND NOT EXISTS (SELECT 1 FROM "DocumentReviews" r WHERE r."ModelType" = 'PurchaseOrder' AND r."ModelId" = po."Id");

            INSERT INTO "DocumentReviewEvents" ("Id", "DocumentReviewId", "Action", "ActorId", "Comments", "At")
            SELECT gen_random_uuid(), r."Id", 0, po."LastUpdatedById", 'Awaiting check when document review was introduced.', r."SubmittedAt"
            FROM "DocumentReviews" r JOIN "PurchaseOrders" po ON po."Id" = r."ModelId"
            WHERE r."ModelType" = 'PurchaseOrder'
                AND NOT EXISTS (SELECT 1 FROM "DocumentReviewEvents" e WHERE e."DocumentReviewId" = r."Id");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql($$"""
        DELETE FROM "DocumentReviewEvents" WHERE "Comments" = 'Awaiting check when document review was introduced.';
        DELETE FROM "DocumentReviews" r WHERE r."ModelType" = 'PurchaseOrder' AND r."Status" = 0
            AND NOT EXISTS (SELECT 1 FROM "DocumentReviewEvents" e WHERE e."DocumentReviewId" = r."Id");
        WITH keys(old_key, new_key) AS (VALUES {{Pairs}})
        DELETE FROM "PermissionTypes" WHERE "Key" IN (SELECT new_key FROM keys);
        WITH keys(old_key, new_key) AS (VALUES {{Pairs}})
        DELETE FROM "roleclaims" WHERE "ClaimType" = 'permission' AND "ClaimValue" IN (SELECT new_key FROM keys);
        """);
}
