# Material retest list and detail read failure

The logs supplied on 2026-10-28 show `MaterialRetestService.ListAsync` failing
with `The Include path 'Material->Batches' results in a cycle`. Both list and
detail reads included the request's batch and its material, while the model
automatically includes every batch on a material. That inverse relationship
creates a cycle which EF Core refuses in a no-tracking query.

The two read queries now use `IgnoreAutoIncludes()` alongside `AsNoTracking()`.
They retain their explicit batch/material and retest-event includes, all DTO
fields, batch filtering, newest-first ordering, chronological event history and
the missing-detail response. The change is local to these two queries; global
material loading and retest writes keep their existing behavior. Endpoint
permissions and the approval, sampling, testing and certificate gates are unchanged.
This fix requires no API contract change or database migration.

`MaterialRetestReadTests` reproduces the original failure using the real context
model, then verifies list and detail results, batch scope, stored values, event
history and lack of tracked entities. Its PostgreSQL test uses only a dedicated
loopback database named `oryx_material_retest_test`, configured through
`ORYX_RETEST_READ_TEST_CONNECTION`. Run it alongside the existing
`MaterialRetestAcceptanceTests` and `MaterialRetestCreationTests`.

The reported Stock approval-configuration warning and the inventory ledger
transaction-type mismatch are separate issues. This query fix does not repair
saved approval stages or alter inventory ledger storage.

## Application-wide validity follow-through (2026-11-04)

Production schedule stock checks now apply `MaterialValidity` on both the live
master and adopted-configuration paths. Allocation, replacement-swap eligibility,
the final reserved-stock issue guard, forecasts, per-warehouse totals, reserved
reports, and both expiry-risk report variants share the same effective date.
Allocation evidence includes retest and material kind so a changed extension
invalidates a previously calculated plan. Date-driven stock selection uses effective
validity while retaining the existing return/priority order. Undated legacy stock
is neither expired nor about to expire. Batch status, availability, ownership,
approvals, stock quantities, issue history and audit requirements remain enforced.

Current-stock reports omit effectively expired batches; expired-stock reports use
the same effective date. Printable warehouse expiry projections calculate their
expiry buckets and ordering from effective validity while retaining original expiry,
retest and effective dates as separate fields for traceability.

Original expiry dates remain recorded and are still available for document/history
fields and explicit original-expiry report filters. Finished-product expiry,
supplier certifications, manually entered QC reagent expiry, import date consistency,
and the governed retest due-date/approval workflow have separate meanings and are
not raw-material stock eligibility checks. None of those dates are rewritten.
No migration, role grant or stock correction is part of this change.

Schedule list/detail visibility uses the authenticated account's current database
department, independently of role order or stale department claims. Production
accounts see only their branch's schedules and products, including legacy mixed
schedules filtered to that branch. Removed schedules/products remain excluded.
Non-production oversight retains existing cross-branch reads; missing departments
are denied. Search and pagination use the same scope. Saved schedule departments,
rather than the creator's subsequently selected department, select pre-start stores.

Regression coverage includes cold-context pre-start reads, elapsed/active expiry
and retest, frozen stock, packaging exclusion, role-independent branch reads,
search/count/detail isolation, and account switching. An optional local PostgreSQL
fixture (`ORYX_ISSUE_REPAIR_TEST_CONNECTION`, loopback hosts only) verifies the real
stock issue, forecast, both risk aggregations, schedule SQL and availability query.
