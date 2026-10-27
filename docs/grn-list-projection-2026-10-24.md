# Lightweight GRN list read — 2026-10-24

## Problem

A list of 21 raw-material GRNs covered 47 linked batches in the inspected live
snapshot. The list hydrated batch/checklist/user graphs and automatic storage,
event and reservation collections. Its shared mapper preloaded sampling and
analysis data but did not pass the data to the per-batch resolvers, which could
repeat six sampling/analysis reads per batch. Mapping was also deferred until
the returned enumerable was consumed during serialization.

## Implementation and API contract

`GET /api/v1/warehouse/grns` keeps its URL, query parameters and authorization.
Its OpenAPI response is now `Paginateable<IEnumerable<GrnSummaryDto>>`.

`GrnListReader` executes at most three queries: filtered total count, ordered
page header scalars, and a single batch-summary read restricted to the page's
GRN IDs. Empty pages stop after the header query. No database calls run inside
record/batch loops or response serialization, and no AutoMapper resolver runs.
Queries suppress incidental AutoIncludes and use no tracking. SQL tags identify
the scope, header and batch-summary reads.

Existing header paths are retained: `id`, `createdAt`, `grnNumber`, `carrierName`,
`vehicleNumber`, `remarks`, `declarationNumber`, and `status`. `materialBatches`
remains an array with one entry per visible batch; each entry carries `id` and
`supplierName`. This preserves existing clients' `.length` counts and the desktop
supplier display while omitting unused QC, inventory, material and user DTOs.
No new frontend hook or manual generated-client edit is required.

Both Next list callers (Quarantine and Raw Materials Request) use these retained
fields. The desktop quarantine list's existing schema also accepts this shape.
Full batch data continues to come from the existing GRN detail endpoint.
`GET /warehouse/grns/qc`, the detail endpoint and their shared enrichment mapper
are unchanged; this optimization is specifically for the warehouse list.

## Validation, scope and workflow

The existing GRN department/deletion filter is preserved verbatim through the
DbContext. Its legacy NonProduction/RnD cross-department policy is not changed.
Active batch/material, checklist and supplier filters remain active. Kind,
status, number/carrier search, pagination totals and batch-count semantics are
retained, including the status filter's existing Any-batch behavior.

Ordering adds ID descending after creation time for stable page boundaries on
timestamp ties. Nonpositive or overflowing pagination inputs return validation
instead of reaching an invalid database offset.

This read does not save entities, mutate inventory, advance quarantine/QC
statuses, bypass approvals, or generate workflow events. Existing request audit
headers and backend authority remain intact. There is no schema migration.

## Verification and delivery

Focused backend tests cover a 47-batch list with no mapper, legacy JSON paths,
department and deletion scope, supported cross-department policy, active batch
and material filtering, supplier visibility and absent checklists, paging,
search, status, empty results and invalid pagination. PostgreSQL-provider SQL
compilation checks that page and batch projections do not include credentials,
storage trees, events, reservations, sampling, analysis, or specifications.
Neighboring warehouse and material-distribution read regressions are included.

Frontend route tests feed the actual minimal summary shape to the desktop list,
verify batch count and deduplicated supplier/carrier display and detail
navigation, and retain the existing quarantine distribution regressions.

Deploy the backend to activate the optimization. Existing frontends can continue
using the same field paths; no frontend release is required. The earlier 8.5 ms
summary SQL prototype was database-only diagnostic evidence, not a measured
latency for this implementation or a promised HTTP response time. No live
write, API deployment, or database configuration change was performed.

Documentation updated: this note, `docs/services.md`, and `docs/workflows.md`.
