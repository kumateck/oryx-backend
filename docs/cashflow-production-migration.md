# Cashflow production migration

Migrations: `20260902065334_AddCashflowPayments` and
`20260903121205_LinkBillingSheetChargePayments`

This is an expand-only migration. It adds nullable columns to existing tables,
adds `Currencies.IsBaseCurrency` with default `false`, and creates
`Payments`, `PaymentApprovals`, `ExchangeRates`, and `InvoiceAmounts`. It does
not rename or drop production data. Legacy backfills only update null targets
when their source is unambiguous.

The later link migration is also expand-only. It adds nullable
`Payments.BillingSheetChargeId`, its index, and a restricted foreign key to
`BillingSheetCharges`. Existing payments and charges remain unchanged.

## Required rollout

1. Take and verify a restorable production backup. Record row counts for
   `Currencies`, `TermsOfPayments`, `BillingSheets`, `ShipmentInvoices`,
   `PurchaseOrderInvoices`, and `Invoices`.
2. Generate an idempotent SQL script from the currently deployed migration to
   `20260903121205_LinkBillingSheetChargePayments`. Review it and execute it first against a
   recent production clone. Do not use application startup to migrate.
3. On the clone, run the validation queries below and exercise payment creation,
   approval, balance, and all three reports. Review every data-quality warning.
4. Schedule the production change, stop concurrent schema deployments, apply
   the reviewed script with database monitoring, then rerun validation.
5. Configure exactly one base currency, seed required historical exchange rates,
   assign the new permissions, and only then enable cashflow UI/report access.
6. Deploy the compatible application. Keep
   `Database:ApplyMigrationsOnStartup=false` in production.

Example script generation (replace the starting migration if production differs):

```sh
dotnet ef migrations script \
  20260901130504_AddPriceAndPriceUoMToShipmentInvoiceItem \
  20260903121205_LinkBillingSheetChargePayments \
  --idempotent --project INFRASTRUCTURE --startup-project API
```

## Read-only validation

```sql
SELECT "MigrationId" FROM "__EFMigrationsHistory"
ORDER BY "MigrationId" DESC LIMIT 5;

SELECT count(*) AS currencies,
       count(*) FILTER (WHERE "IsBaseCurrency") AS base_currencies
FROM "Currencies";

SELECT count(*) AS payments FROM "Payments";
SELECT count(*) FILTER (WHERE "BillingSheetChargeId" IS NOT NULL)
       AS linked_billing_sheet_charge_payments
FROM "Payments";
SELECT count(*) AS payment_approvals FROM "PaymentApprovals";
SELECT count(*) AS exchange_rates FROM "ExchangeRates";
SELECT count(*) AS invoice_amounts FROM "InvoiceAmounts";

SELECT count(*) FILTER (WHERE "DueDays" IS NULL) AS unknown_due_terms
FROM "TermsOfPayments";
SELECT count(*) FILTER (WHERE "DueDate" IS NULL) AS unknown_invoice_due_dates
FROM "Invoices";
```

Compare all pre-migration entity counts with post-migration counts. Existing
entity counts must not decrease.

## Rollback

If the application fails after schema deployment, roll back the application and
leave the additive schema in place; the prior version ignores it. Once any
payments, approvals, rates, or invoice snapshots exist, do **not** run the EF
`Down` migration because it drops those new tables and would lose data. Resolve
forward with a corrective migration. A destructive schema rollback is permitted
only before the feature writes data, from the reviewed script, with a verified
backup and explicit operations approval.
