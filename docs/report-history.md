# Recorded report history

The Live report upgrade adds optional `asOfDate` to marked read endpoints and an authenticated `/api/v1/report/history/capabilities` endpoint. Current requests retain existing behavior. Historical requests require a currently enabled report permission, checked against live grants before and after the read. Authentication roles, claims and tokens are never replayed.

## Rollout

1. Review the backend change and take a normal PostgreSQL backup, including the new history tables in ongoing backups.
2. Exercise migration `20261027170000_ReportHistoryCapture` against a restored staging copy first. It locks reporting source tables while taking a consistent opening baseline; schedule the production migration in the existing approved maintenance procedure. Large tables require corresponding disk space and migration time.
3. Deploy the companion backend and run its normal migration process. This repository change does not run customer migrations or contact customer services.
4. Verify the capabilities response using an account with a report permission: protocolVersion 1, basis RecordedState, available true, availableFrom and serverTime in UTC.
5. Deploy the frontend. Existing clients and current reports continue working; older backend versions do not provide historical reporting.

No pre-baseline balances or HR assignments are fabricated. A date before the latest required baseline returns 409 with availableFrom. Later business corrections appear from when they were recorded; they are not backdated into an earlier snapshot. A UTC date from Live means end of that day, capped at server time for today. A generation uses one frozen instant across requests.

## Integrity and operations

History tables are additive, SQL-managed operational tables outside the EF entity model. Deferred triggers capture row insert/update/delete in the same transaction, including raw SQL and bulk writes. Rolled-back transactions leave no history. Stable keys include composite primary keys. Reporting roots and their foreign-key reference dependencies are captured; owned/auto-included collection roots are declared explicitly. Credential fields are removed from users payloads, and grant/token tables are excluded.

History is append-only. Source and history TRUNCATE operations are rejected because they would create gaps; ordinary deletions remain captured. Do not disable triggers or manually delete/truncate history. Use monitored backups and capacity planning; no automatic retention deletion is introduced. Restore source data and history consistently. Reinstalling the migration preserves existing baseline dates and evidence. Its Down deliberately retains capture/history so an application rollback does not erase evidence or open a capture gap.

Historical reads use connection-local temporary views in a repeatable-read transaction and reuse the existing reporting calculations, soft-delete filters and joins. Views shadow reporting source tables only on that connection. The read guard rejects writes and joins to unversioned dependencies or explicitly schema-qualified live tables. The transaction is rolled back after the action, removing views and restoring the connection search path. SaveChanges is prohibited inside a history scope. Missing coverage fails closed rather than mixing current and historical records.

New report dependencies or schema changes need an explicit history review. Add any required collection roots and a new additive migration using ReportHistorySchema.InstallSql; the coverage start for newly captured tables bounds the combined history. A column added after baseline cannot be represented as historical fact before it was recorded; such changes require a migration/coverage policy review rather than assuming old JSON provides the new field. Database superusers remain responsible for operational integrity as with other audit records.

## Local verification

The PostgreSQL tests opt in only to an explicitly disposable, loopback database whose name starts with `oryx_report_history_test`. They drop/recreate public schema there. Never point them at an application database. Use a dedicated PostgreSQL container, a published loopback port and max_locks_per_transaction=1024 for creating/cleaning up the full ERP fixture schema. Remove that owned fixture after the run.

Run with ORYX_REPORT_HISTORY_TEST_CONNECTION set to that local fixture:

```bash
dotnet test tests/APP.Tests/APP.Tests.csproj --filter 'FullyQualifiedName~ReportHistory|FullyQualifiedName~ReportRepository|FullyQualifiedName~ProductionActivityBoardTests'
```

Tests verify old/current stock and staff grades, renamed departments, rollback/deletion, credential exclusion, prepared statement isolation, read-only protection, complete-model report dependencies and live permission denial. The final-packing read fix returns FinalPacking.NotFound (404) for an absent record instead of Error.NullValue (400); it performs no writes or workflow transitions.

Report lookups use existing report data access instead of requiring inventory/vendor administration grants. `/report/history/vendors` returns only names/IDs, requires the store/inventory report permission on current and historical reads, and supports the same recorded instant. The item master report now maps actual item code, stock, limits, active state and UOM symbol instead of leaving those values at DTO defaults. Approved warehouse material reporting excludes production-floor and finished-goods locations.
