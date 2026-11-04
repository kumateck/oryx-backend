# Migration top-ups and retest corrections

This integration adds additive material-stock migration drafts and audited
raw-material migration retest corrections. It excludes the separate White
Placebo implementation and preserves the production-material repair model on
`desmond-latest`.

## Schema and rollout

Apply the migrations through the normal reviewed database release process:

- `20261103120825_AddMigrationStockTopUps`: mode/reason on migration drafts and
  immutable before/added/after balance evidence. Existing drafts default to the
  opening-balance replacement mode.
- `20261103123050_AddMigrationRetestDateAudit`: before/after retest-date audit
  records with actor, reason and timestamp.

Both migrations were regenerated against the target branch model without White
Placebo tables or its material flag. No customer migration is applied by local
builds, tests or Git merges. Deploy the matching API before the desktop UI.

## Top-up behavior

`POST /api/v1/stock-migrations/top-ups` saves a manual list; the sibling
`top-ups/upload` endpoint reads the existing stock workbook format. Both require
view and upload opening-balance permissions, the actor's department scope and a
nonempty reason. Neither creates available stock. Existing correction/recheck/
discard actions and the separate approval permission apply to these drafts.

Approval validates the full list and creates separate stock lots atomically,
without writing off or merging existing lots. Draft and department locks prevent
repeat/concurrent posting. Each included row retains its imported batch ID;
different GRN/AR references keep separate receipt identities. A supplier batch is
required for a top-up, with no GRN substitution. Units and month-based dates use
the existing migration contract. Bin-card receipts continue the material/store
ledger balance. Approval totals remain fixed after later inventory use.

## Retest behavior

For raw material, active original expiry takes priority; once it elapses, a
future recorded retest date can extend usability. Packaging retains its expiry
rule. Quality, stock, approval and department gates still apply. The shared
validity expression governs stock selection, issue/reservation writes, expiry
reports and background checks.

`PUT /api/v1/material/batch/{batchId}/migration-retest-date` requires
`CanUpdateMigrationRetestDate`, available approved migration stock in the actor's
department and no existing governed QC retest cycle. A reason and expected date
values bind the correction to the reviewed version. Audit and date changes save
in one transaction. This correction does not complete a QC retest or alter stock
quantities, original expiry or approval status. No role receives the new grant
automatically.

Expiry lists accept optional code/name search and optional date bounds before
pagination. They retain their expired/soon classifications when bounds are
cleared.

## Local verification

Use the focused stock migration, top-up, validity, migration correction and QC
retest acceptance suites. `StockTopUpPostgresTests` additionally accepts
`ORYX_STOCK_TOPUP_TEST_CONNECTION` pointing to a disposable PostgreSQL server
with CREATE DATABASE rights. It creates/drops a unique test database and checks
draft isolation, receipt/batch links, cumulative bin cards, repeat/concurrent
approval, approved-edit refusal, blocked rows, transaction rollback and immutable
approval comparisons. Never use a customer database for that fixture.

Run EF's `migrations has-pending-model-changes` against the built infrastructure
project to check model/snapshot consistency without applying a migration. Native
Windows acceptance and real PostgreSQL contention for migration retest-date
corrections remain staging checks.

The isolated migration-only integration passed 70 focused backend tests on
3 November 2026 with the PostgreSQL fixture enabled. The build passed, EF reported
no pending model changes, and the 25 owned migration API paths/schemas matched
the desktop import exactly after the documented audit-header normalization.
