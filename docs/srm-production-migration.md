# SRM production migration

Migration: `20260902121047_AddSupplierRelationshipManagement`

This migration is additive. It adds nullable `ApprovedAt` and
`RequalificationDueDate` columns to `Suppliers` and creates five new tables:
`SupplierCertifications`, `SupplierContacts`, `SupplierBankDetails`,
`SupplierPricingAgreements`, and `SupplierPerformanceRecords`. Existing supplier
identity/contact columns and the separate Vendor subsystem are unchanged.

## Required rollout

1. Verify a restorable backup and record row counts for `Suppliers`, `Payments`,
   `ShipmentDocuments`, `PurchaseOrders`, `Checklists`, and `MaterialBatches`.
2. Apply the prerequisite Cashflow and retest migrations on a recent production
   clone, then generate an idempotent script ending at the SRM migration.
3. Review the script and confirm it contains only new nullable columns, tables,
   foreign keys, and indexes. Exercise every SRM endpoint on the clone.
4. Run the validation queries below. Reconcile existing approved suppliers with
   null qualification dates through an authorized data-cleansing process; the
   migration deliberately does not invent dates.
5. Apply the reviewed script during a controlled deployment window. Keep
   `Database:ApplyMigrationsOnStartup=false`.
6. Assign the five SRM permissions before enabling the UI or integrations.

Example script generation:

```sh
dotnet ef migrations script \
  20260902075549_AddRetestFieldsAndIsRetestFlag \
  20260902121047_AddSupplierRelationshipManagement \
  --idempotent --project INFRASTRUCTURE --startup-project API
```

## Read-only validation

```sql
SELECT "MigrationId" FROM "__EFMigrationsHistory"
ORDER BY "MigrationId" DESC LIMIT 5;

SELECT count(*) AS suppliers,
       count(*) FILTER (WHERE "Status" = 1) AS approved,
       count(*) FILTER (
         WHERE "Status" = 1 AND "RequalificationDueDate" IS NULL
       ) AS approved_requiring_date_review
FROM "Suppliers";

SELECT count(*) FROM "SupplierCertifications";
SELECT count(*) FROM "SupplierContacts";
SELECT count(*) FROM "SupplierBankDetails";
SELECT count(*) FROM "SupplierPricingAgreements";
SELECT count(*) FROM "SupplierPerformanceRecords";

SELECT "SupplierId", count(*)
FROM "SupplierContacts"
WHERE "DeletedAt" IS NULL AND "IsPrimary" = TRUE
GROUP BY "SupplierId" HAVING count(*) > 1;
```

Existing entity row counts must not decrease. The final primary-contact query must
return no rows.

## Rollback

If the application must be rolled back, deploy the prior application and leave the
additive schema in place. Once any certification, contact, bank, agreement, or
scorecard data exists, do not run the EF `Down` migration because it drops those
tables. Correct forward with another migration. Destructive rollback is allowed
only before feature writes, with a verified backup and explicit operations
approval.
