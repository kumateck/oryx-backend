# Batch reservation repair

`GET` and `POST /api/v1/production-schedule/{batchId}/reservation-repair` expose
`PreviewReservationRepair` / `ApplyReservationRepair`. Both require
`CanViewPlannedProducts` and the dedicated `CanRepairStockReservations` permission,
an active account and a batch in the actor's current production department.
The capability is registered under Production / Products; it is not granted to
any role by this implementation. The session supplies the actor.

The preview selects the current approved, non-deleted opening-balance replacement
and shows its file plus affected unissued reservations, old/replacement lots and
shelves, quantities, available stock, blockers and history. Approval-time ties
require review. Migration-written-off lots need retained write-off evidence;
physically missing batches cannot be reconstructed by guessing materials.

The SQL plan is an embedded resource owned by `ReservationRepairService`. It
retains the maintenance repair's approval, issue-history, quantity/UOM, production,
lot validity, unique allocation and bin-card reconciliation checks. Capacity
includes the department's other waiting migration-stale reservations, while only
the selected batch's reservations are rebound. Existing BOM/packaging repair
permissions do not authorize this operation. Normal issue permissions still
govern subsequent stock issue; this repair never issues stock.

Apply accepts a request UUID, exact preview token and a 10–1000 character reason.
It locks business/access evidence before rebuilding the plan, rechecks access
before writing, and commits reservation bindings, activity log and immutable audit
together. Reservation IDs, quantities, production warehouse, stock, approvals,
statuses and dates are preserved. A replay returns the prior receipt after live
access checks; conflicting request-ID reuse and stale snapshots return conflicts.

`20261105113842_AddReservationRepairs` adds the receipt table with restrictive
foreign keys and a database trigger rejecting UPDATE, DELETE and TRUNCATE. EF
also rejects receipt edits/deletes. Its before/after evidence and reviewed rows
remain available in preview history. The backend migration must precede the
companion desktop UI. No customer migration, role assignment or deployment is
performed by local implementation.

Run integration tests against an isolated local PostgreSQL instance:

```sh
ORYX_ISSUE_REPAIR_TEST_CONNECTION='Host=localhost;Port=5432;Database=postgres;Username=LOCAL_USER' \
  dotnet test tests/APP.Tests/APP.Tests.csproj --filter FullyQualifiedName~ReservationRepairTests
```

The fixture refuses remote hosts and creates/drops uniquely named disposable
databases. Tests exercise the exact deployment migration, deleted migration
evidence, raw/packaging retest precedence, permission isolation, stock invariants,
whole-queue capacity, issue/production/ledger blockers, rollback and concurrent
replay. On 5 November 2026, the clean PR branch passed 14 new tests and 19 existing
issued-material / production-unstart tests, run serially with no skips.
