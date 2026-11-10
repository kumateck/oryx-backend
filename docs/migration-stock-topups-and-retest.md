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

Approval validates the full list and atomically adds to a matching lot or creates
a separate lot without writing off existing stock. Draft and department locks prevent
repeat/concurrent posting. Each included row retains its imported batch ID;
different GRN/AR references keep separate receipt identities. A supplier batch is
optional for a top-up; an unnumbered row warns and remains a separate lot, with
no GRN substitution. Units and month-based dates use
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

## Calendar months and shelf locations (November 2026)

Manufacturing, expiry and retest inputs use month/year. This applies to manual
entry, Excel, draft corrections and old drafts carrying `exactDates: true`.
The compatibility flag is accepted but does not select day-level matching.
Working manufacturing/expiry values use the month start; posted expiry and
retest use the month end, including February in leap years. Original `Sheet*`
values remain evidence. A re-check/approval normalizes working draft dates;
approved import rows and existing lot dates are not rewritten.

Top-ups match material, supplier batch, AR, unit and all three date months in
the department's stores. Matching rows increase the existing lot and shelf
quantity, retaining receipt/bin-card/movement/import evidence. Different months
or AR references remain separate. A missing batch number is a warning only:
the row can be approved, and each unnumbered row remains its own lot.

Deploy backend and desktop changes together. Existing approved split lots are
not silently reparented or merged: their bin cards, reservations and import
references are historical evidence. Do not delete or re-import these rows to
combine the display; that would risk double-counting stock.

Stock-by-batch excludes deleted and empty shelf allocations before pagination.
The batch expiry page lists each current shelf quantity and active reservation
warehouse separately. A batch total is not necessarily stock still on shelves.
For example, an empty former shelf and 215 L reserved on a production floor
must show the production allocation rather than claim the stock is unlocated.
The desktop shelf action uses the existing per-kind reassign permission and
`POST /api/v1/material/batch/move/v2`; permissions and audit behavior are retained.
No schema migration or new permission is required for the shelf action.

## Repair an already approved split top-up

Approved migration rows and imported lots are historical evidence. Do not edit
or delete the import, bin-card, movement, or reservation rows to make the UI
appear consolidated. When two existing lots meet the strict matching rules,
`deploy/repairs/consolidate-approved-topup.sql` provides a database-operator
preview/apply repair. It does not run automatically during deployment.

Back up the database first and run the script from a trusted host with
`psql -X -v ON_ERROR_STOP=1`. Supply `source_batch_id`, `target_batch_id`,
`expected_database`, `actor_email`, `reason`, `apply`, and `expected_token` as
psql variables; the script converts them to SQL literals. Use `apply=false`
and an empty token for the preview. Review every reported lot, shelf, reference,
blocker and the returned `previewToken`. For apply, repeat the exact inputs with
`apply=true` and that token. The transaction aborts if any reviewed data changed.

The source must be an unused approved top-up lot, with all quantity on the
operator's department shelves. The lots must match material, supplier batch,
AR references, unit and manufacturing/expiry/retest months. Cross-department
stock, reservations, unsupported foreign-key references, QC history, or
insufficient bin-card balance block the repair for manual review. On apply the
script moves shelf quantity and ledger entries, preserves source import history,
marks the empty source lot consumed, and writes immutable before/after evidence
to `_MigrationTopUpConsolidations`. It installs a protection trigger on that
evidence table. No customer data was changed as part of preparing this script.

The script checks the operator's active warehouse department and one of two
new sensitive permissions in Settings → Opening Balances:
`CanConsolidateRawMaterialTopUps` or `CanConsolidatePackagingMaterialTopUps`.
They are registered with no automatic role grants. Assign the appropriate key
only to an authorized operator before running apply. Review the preview against
the approved migration records and current stock ledger; if it reports a
blocker, stop and reconcile manually rather than broadening the script.
