using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace INFRASTRUCTURE.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261121154214_GrantFinishedGoodsWorkflowTasks")]
public class GrantFinishedGoodsWorkflowTasks : Migration
{
    // These tasks split the previous PO-attachment/auto-invoice and delivered actions.
    // New finance clearance, stock reconciliation, cancellation, void and recall powers
    // are granted to nobody. Administrators must assign their separate task keys.
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        INSERT INTO "roleclaims" ("RoleId", "ClaimType", "ClaimValue")
        SELECT DISTINCT source."RoleId", 'permission', keys.task
        FROM "roleclaims" source CROSS JOIN (VALUES
            ('CanVerifyFinishedGoodsCustomerPo','CanSendProformaInvoice'),
            ('CanDispatchFinishedGoods','CanDeliverFinishedGoods'),
            ('CanPrintFinishedGoodsWaybill','CanViewWaybillForFgw'),
            ('CanApplyFinishedGoodsStockAdjustment','CanCreateMaterialStockAdjustment'),
            ('CanApplyFinishedGoodsStockAdjustment','CanApprovePendingApproval'),
            ('CanViewApprovedProducts','CanViewFgtn'),
            ('CanViewInvoice','CanViewPayments'),
            ('CanViewInvoice','CanViewCashflowReports')) keys(task,previous)
        WHERE source."ClaimType" = 'permission' AND source."ClaimValue" = keys.previous
        AND NOT EXISTS (SELECT 1 FROM "roleclaims" existing WHERE existing."RoleId" = source."RoleId"
            AND existing."ClaimType" = 'permission' AND existing."ClaimValue" = keys.task);

        INSERT INTO "PermissionTypes" ("Id", "Key", "RoleClaimId", "Type")
        SELECT DISTINCT ON (target."Id", granted."Type") gen_random_uuid(), target."ClaimValue", target."Id", granted."Type"
        FROM "roleclaims" source JOIN "PermissionTypes" granted ON granted."RoleClaimId" = source."Id"
        CROSS JOIN (VALUES ('CanVerifyFinishedGoodsCustomerPo','CanSendProformaInvoice'),
            ('CanDispatchFinishedGoods','CanDeliverFinishedGoods'),
            ('CanPrintFinishedGoodsWaybill','CanViewWaybillForFgw'),
            ('CanApplyFinishedGoodsStockAdjustment','CanCreateMaterialStockAdjustment'),
            ('CanApplyFinishedGoodsStockAdjustment','CanApprovePendingApproval'),
            ('CanViewApprovedProducts','CanViewFgtn'),
            ('CanViewInvoice','CanViewPayments'),
            ('CanViewInvoice','CanViewCashflowReports')) keys(task,previous)
        JOIN "roleclaims" target ON target."RoleId" = source."RoleId" AND target."ClaimType" = 'permission' AND target."ClaimValue" = keys.task
        WHERE source."ClaimType" = 'permission' AND source."ClaimValue" = keys.previous
        AND NOT EXISTS (SELECT 1 FROM "PermissionTypes" existing WHERE existing."RoleClaimId" = target."Id" AND existing."Type" = granted."Type");
        """);
    protected override void Down(MigrationBuilder migrationBuilder) { /* Never remove later administrator grants. */ }
}
