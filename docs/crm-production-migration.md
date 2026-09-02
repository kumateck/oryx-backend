# CRM production migration

Migration: `20260902125444_CatchUpPendingModelChanges`

Despite its generated name, this migration contains the CRM schema change only.
It is additive: nullable customer and ProductionOrder columns, nullable negotiated
fields on legacy production-order lines (`DiscountPercent` defaults to zero), and
five new tables for contacts, pricing, quotations, quotation items, and quotation
approvals. It does not drop, rename, truncate, delete, or rewrite existing rows.

## Required rollout

1. Verify a restorable backup and record row counts for `Customers`, `Invoices`,
   `InvoiceAmounts`, `Payments`, `ProductionOrders`, and
   `ProductionOrderProducts`.
2. Restore a recent production backup into an isolated clone. Apply pending
   prerequisites through `20260902121047_AddSupplierRelationshipManagement`.
3. Generate and review an idempotent script ending at the CRM migration. Confirm
   it contains only `ADD COLUMN`, new tables, indexes, foreign keys, and the
   migration-history insert.
4. Apply it to the clone and run the read-only validation below. Exercise customer
   CRUD, credit status, pricing boundaries, maker-checker approval, and quotation
   conversion on the clone.
5. Configure exactly one approval workflow with `ItemType = CustomerQuotation`
   and at least one assigned stage. Assign all six CRM permissions before clients
   are enabled.
6. Apply the reviewed script in a controlled window. Keep
   `Database:ApplyMigrationsOnStartup=false`; never let an application restart
   choose when production schema changes.

Example script generation:

```sh
dotnet ef migrations script \
  20260902121047_AddSupplierRelationshipManagement \
  20260902125444_CatchUpPendingModelChanges \
  --idempotent --project APP/APP.csproj --startup-project API/API.csproj
```

## Legacy-data policy

- Existing `Address` remains unchanged. New billing and shipping addresses stay
  null until an authorized user verifies them.
- Existing customers retain null credit limits, meaning no limit is enforced.
  Currency and payment terms remain null until reviewed; the migration does not
  guess commercial terms.
- Existing ProductionOrder lines retain null `UnitPrice` and continue using the
  legacy Product price fallback. Do not backfill a negotiated price from today's
  product price because that would rewrite historical commercial meaning.
- Existing orders have null promised-delivery and quotation-source fields.

## Read-only validation

```sql
SELECT "MigrationId" FROM "__EFMigrationsHistory"
ORDER BY "MigrationId" DESC LIMIT 6;

SELECT count(*) AS customers,
       count(*) FILTER (WHERE "CreditLimit" IS NOT NULL) AS limited_customers,
       count(*) FILTER (
         WHERE "CreditLimit" IS NOT NULL AND "CurrencyId" IS NULL
       ) AS invalid_limited_customers
FROM "Customers";

SELECT count(*) FROM "ProductionOrders";
SELECT count(*) FROM "ProductionOrderProducts";
SELECT count(*) FROM "CustomerContacts";
SELECT count(*) FROM "CustomerPricingAgreements";
SELECT count(*) FROM "CustomerQuotations";
SELECT count(*) FROM "CustomerQuotationItems";
SELECT count(*) FROM "CustomerQuotationApprovals";

SELECT "CustomerId", count(*)
FROM "CustomerContacts"
WHERE "DeletedAt" IS NULL AND "IsPrimary" = TRUE
GROUP BY "CustomerId" HAVING count(*) > 1;

SELECT "SourceCustomerQuotationId", count(*)
FROM "ProductionOrders"
WHERE "SourceCustomerQuotationId" IS NOT NULL
GROUP BY "SourceCustomerQuotationId" HAVING count(*) > 1;
```

Existing entity counts must not decrease. Both duplicate-detection queries and
`invalid_limited_customers` must return zero.

## Rollback

If application rollback is required, deploy the prior application and leave the
additive schema in place. Once any CRM row or quotation link exists, do not run
the EF `Down` migration because it drops CRM tables and columns. Correct forward
with another reviewed migration. Destructive rollback is allowed only before any
feature write, with a verified backup and explicit operations approval.
