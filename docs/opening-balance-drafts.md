# Opening-balance draft workflow

Material stock uploads use `StockMigrationController`: uploading persists a draft
and check results without posting stock. The legacy material `upload/stock`
action refuses direct stock writes with `Stock.DraftRequired`.

All migration endpoints require `CanViewOpeningBalances`. Upload additionally
requires `CanUploadOpeningBalances`; correction, bulk updates, file-unit changes,
recheck and discard require `CanCorrectOpeningBalances`; approval requires
`CanApproveOpeningBalances`. Keys are registered under Settings / Opening Balances.
Existing department ownership checks remain. No role grants are added. A user may
hold both correction and approval permissions; distinct-person approval is not
enforced by this change.

Rows with missing department-material links, incompatible quantity families,
future manufacture, or manufacture after expiry block approval. Expired stock is
retained with a warning. Within one material and batch, earliest manufacturing and
latest expiry are reconciled and recorded in correction notes. Expiry is stored
at month end. Quantities are interpreted using the draft's declared file mass and
volume units and stored using its material-department linkage.

Draft mutations and approval acquire the same PostgreSQL transaction advisory
lock for that draft ID. The status claim, prior-store write-off, new batches,
shelf quantities, bin cards and approval metadata commit together. Approval
rechecks against current masters and refuses blocked rows. Approved/discarded
drafts cannot be changed or approved again. Approval retains the existing
replacement behavior for the department/material kind; users must review the
comparison and confirmation before posting.

Supplier batch and GRN values may be absent from a counted opening-stock row.
Leave those values blank rather than inventing identifiers. Such a row carries
a review warning and posts as its own internal stock record; separate unbatched
source rows are never merged, reconciled to each other's dates, or counted as
one batch in the comparison. Existing supplier batches retain the normal
same-material/batch grouping and date validation. Excel-damaged batch values
still block approval. AR references and the migration line-to-stock record link
remain available for traceability. This rule does not change material masters'
batch settings or the receiving/production workflows.

The legacy repository importer initializes a new batch total at zero before its
normal per-row accumulation, correcting the doubled first quantity. Direct HTTP
access to that importer is disabled; repository regression coverage remains.

No schema or DTO changes are required. Rebuild/restart both backend and frontend
before using the new permissions and flow. The desktop operator guide is
`oryx-live-desktop/docs/fresh-opening-balances.md`; it records the authorized
1 November 2026 local reset, backup, complete table manifest and four stock files.

AR references are read from the workbook's `AR No.`, `AR Number(s)` or
`ARD Number(s)` column. Available values are retained in `ArNumber`, returned
with draft rows and searchable case-insensitively. Blank, N/A, nil, none and
literal null markers become database null, with no fabricated replacement.
When the dedicated cell is unavailable, an AR explicitly supplied alongside
the GRN is still retained. Posting carries supplied AR values to bin cards.

## Verification

`OpeningBalanceDatesTests`, `OpeningBalancePermissionTests`,
`MaterialStockImportTests`, and existing `StockMigrationSheetTests` cover the
reconciliation rule, distinct policies/catalog, disabled direct upload,
first-row count regression and sheet parsing.

`OpeningBalanceBatchesTests` verifies that known batch identities still group
case-insensitively and absent batches keep source rows separate.
`OpeningBalanceUnbatchedImportTests` uploads two real workbook rows through the
repository, verifies blank batch/GRN retention, independent dates and AR values,
two comparison stock records and zero stock/bin-card writes. The focused
migration suite passes 31 tests.

On 2 November, three additional prepared workbooks (84 Ointment packaging,
57 Beta raw and 96 Beta packaging rows) were trial-uploaded through the actual
repository in `oryx_migration_20261101`. Every prepared quantity was verified.
Both trials rolled back all draft writes and any temporary department-link/UOM
setup. Five blank source material codes intentionally remain approval blockers;
no source code was substituted. No approval, client deployment or stock posting
was performed by that trial.

`OpeningBalanceRehearsalTests` is an opt-in integration rehearsal against the
hardcoded disposable database `oryx_rehearsal_20261101_203716`. Enable only with
`ORYX_OPENING_BALANCE_REHEARSAL=1`, set `ORYX_OPENING_BALANCE_FILES` to the prepared
four-file directory and `ORYX_OPENING_BALANCE_REPORT` to its JSON report path.
It never targets the main database. The database must first be restored/reset
with the reviewed transaction cleanup and approved master setup in place.

The completed rehearsal uploaded 819 rows as drafts with zero stock. Four known
tablet date errors blocked approval; those rows were excluded only on the
rehearsal copy, then 815 rows posted with batch/shelf/bin-card reconciliation.
Repeat posting and correction after approval failed as expected. Master counts,
13 distinct Folidyn batches and 133 expired batches were preserved. The main
database remains empty of stock and awaits the user's own uploads.
