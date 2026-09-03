# Quality Audit production migration

Migration: `20260903155427_AddQualityAuditModule`

This migration is purely additive. It creates seven new tables -
`QualityAudits`, `QualityAuditTeamMembers`, `AuditChecklistTemplates`,
`AuditChecklistTemplateItems`, `AuditChecklistResponses`, `AuditFindings`, and
`AuditCorrectiveActions` - along with their foreign keys and indexes. No
existing table, column, or index is altered or dropped. Evidence attachments
reuse the existing generic `Attachments` table and Minio blob storage; no new
storage schema was needed for that.

## Required rollout

1. Verify a restorable backup and record row counts for `Users`,
   `ProductionOrders`, `Materials`, `Products`, and `Suppliers` (the tables the
   new optional foreign keys reference).
2. Generate an idempotent script ending at this migration and review it -
   confirm every operation is `CreateTable`, `CreateIndex`, or `AddForeignKey`.
3. Apply the reviewed script during a controlled deployment window. Keep
   `Database:ApplyMigrationsOnStartup=false`, consistent with every other
   module in this codebase.
4. On first boot after the schema is in place, temporarily set
   `Database:ApplyMigrationsOnStartup=true` for one deploy so the
   `AuditChecklistTemplateSeeder` runs and seeds the two starter checklist
   templates ("WHO GMP Self-Inspection - Manufacturing Area" and "Supplier
   Quality Audit"). The seeder is idempotent (it no-ops if any template
   already exists), so this is safe to leave on or repeat. Set the flag back
   to `false` afterwards.
5. Assign the six Quality Audit permissions (`CanViewQualityAudits`,
   `CanCreateQualityAudit`, `CanConductQualityAudit`,
   `CanRaiseCorrectiveAction`, `CanReviewCorrectiveAction`,
   `CanCloseQualityAudit`, all under the new `Audit` module) to the
   appropriate roles before enabling the "Audit" sidebar section for users.

Example script generation:

```sh
dotnet ef migrations script \
  20260903121205_LinkBillingSheetChargePayments \
  20260903155427_AddQualityAuditModule \
  --idempotent --project INFRASTRUCTURE --startup-project API
```

## Read-only validation

```sql
SELECT "MigrationId" FROM "__EFMigrationsHistory"
ORDER BY "MigrationId" DESC LIMIT 5;

SELECT count(*) FROM "QualityAudits";
SELECT count(*) FROM "QualityAuditTeamMembers";
SELECT count(*) FROM "AuditChecklistTemplates";
SELECT count(*) FROM "AuditChecklistTemplateItems";
SELECT count(*) FROM "AuditChecklistResponses";
SELECT count(*) FROM "AuditFindings";
SELECT count(*) FROM "AuditCorrectiveActions";

-- Every finding whose CorrectiveAction is Closed should belong to a Closed finding.
SELECT f."Id"
FROM "AuditFindings" f
JOIN "AuditCorrectiveActions" c ON c."AuditFindingId" = f."Id"
WHERE c."Status" = 3 AND f."Status" <> 2;

-- No audit should be Closed while a linked corrective action is still open.
SELECT a."Id"
FROM "QualityAudits" a
JOIN "AuditFindings" f ON f."QualityAuditId" = a."Id"
JOIN "AuditCorrectiveActions" c ON c."AuditFindingId" = f."Id"
WHERE a."Status" = 4 AND c."Status" <> 3;
```

Existing entity row counts (`Users`, `ProductionOrders`, `Materials`,
`Products`, `Suppliers`) must not decrease. Both consistency queries above
must return no rows.

## Rollback

If the application must be rolled back, deploy the prior application and leave
the additive schema in place. Once any audit, finding, or corrective-action
data exists, do not run the EF `Down` migration - it drops all seven tables.
Correct forward with another migration instead. Destructive rollback is
allowed only before feature writes, with a verified backup and explicit
operations approval.
