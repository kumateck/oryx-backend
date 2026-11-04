# Production unstart repair

`ProductionUnstartService` provides a separate permissioned correction for one
accidentally started run before BMR issue. `GET`/`POST`
`/api/v1/production-schedule/activity/{activityId}/unstart` expose typed preview
and apply operations. The existing board view permission, new
`CanUnstartProduction` permission, active account and product department are
checked before reading, under the write transaction and before later writes.
No administrator bypass or automatic role grant is added.

The repair requires matching draft BMR/BPR records on the initial step, both
without issuer or issue date, with no sampling/quality progression. Only the
initial draft preparation step may have started. Later/completed steps,
finished/cancelled run or schedule, invalid/deleted/consumed reservations or any
dependent business records block the action. Dependency inspection follows EF
foreign keys to the batch, run, steps, BMR/BPR and reservations; it includes
soft-deleted requisitions, analytical/form records, returns, transfers, packing
and correction history. Unissued reservations must still fit source shelves.

One serializable transaction archives the run, steps and unissued documents,
releases reservations with `ProductionUnstartRepairId`, reconciles the schedule
against its other active runs, and saves an immutable repair receipt and activity
log. Physical shelves, received/consumed totals, movements and bin cards remain
unchanged. Before/after snapshots retain the changed state and IDs. The database
and context refuse changes to repair receipts. Released reservations remain
history and are excluded from consumption reads and later issued-stock repair
plans. Old logs/step configuration remain attached to the archived run.

Active activity/step queries exclude archived starts. The run-per-batch unique
index is partial (`DeletedAt IS NULL`), allowing a new run without deleting its
old evidence. Start operations serialize before checking for an active run and
reserve within the same transaction after creating the run. Existing batch
numbers are retained when new draft documents are created.

Apply takes `requestId`, `previewToken` and a required reason. An identical or
new request for an already-unstarted activity returns the original receipt;
it never targets a later restarted run. Conflicting request ID reuse and stale
previews fail. Rollback clears pending tracked changes so another scoped save
cannot flush a refused correction.

The activity, step and draft `DeletedAt` fields are concurrency tokens. Migration
guards reject late references to archived run/documents and edits to archived
history, plus new reservations against an unstarted batch before restart.
BMR issue no longer ignores archival filters. BMR/BPR issuance reports a conflict
on stale drafts. A race cannot create an issued archived document.

Apply `20261103213221_AddProductionUnstartRepair` only as part of a coordinated
backend/UI activation, after the other prepared migrations. It adds the audit
table, release marker, partial index, triggers and functions. No existing
customer/main/rehearsal database or role was changed by implementation.

The `ProductionUnstartTests` PostgreSQL suite uses the existing opt-in
`ORYX_ISSUE_REPAIR_TEST_CONNECTION`, restricted to local servers, creates random
`oryx_issue_repair_test_*` databases and removes them afterward. It exercises the
full migration Up from its baseline, real restart with the repository/mappers,
repeat repair, two successive unstarts, reservation reporting, access/staleness,
dependent records, another live run, SQL audit protection, rollback, late writers
and concurrent BMR issue. Run it alongside existing production-board, step
record and issued-stock repair tests before release.
