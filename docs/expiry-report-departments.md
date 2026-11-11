# Department-scoped material expiry reports

Both expired and about-to-expire endpoints retain their report permission and
request contract. The controller passes the authenticated actor to the repository;
the repository reads the current active account rather than accepting a department
from the caller or using a stale department claim. Assigned departments limit
stock-owning warehouses before pagination and counting. Search, dates, kind and
warehouse filters intersect that scope. Missing or disabled accounts are refused.

StockDepartments is an additive MaterialBatchDto read field listing distinct
department IDs/names/codes from positive shelf stock and active reservations.
Zero, deleted or released allocations do not establish ownership. Material-use
associations do not qualify a department. Unknown ownership is not guessed.

Assigned-department report rows restrict shelf/reservation/location collections and
current stock quantities to that department. Total is currently held shelf plus
reserved stock, remaining is shelf stock, and movement/consumption fields are zero
for this current-position report. The original stored batch and audit/history are
unchanged. Users without a department retain the existing global report, subject
to endpoint permissions. No schema migration or write operation is introduced.

Deploy this backend before the companion UI; older UIs safely ignore the additive
department metadata. Verification covers expired/soon, raw/packaging, shared lots,
pagination counts, parameter isolation, account reassignment, and disabled accounts.


## Printable current and expired stock

`GET /api/v1/material/batches/stock-report` is a current-only read under
`CanViewWarehouseReport`, with optional department/material-kind and expired-only
filters. Current account ownership overrides global scope; an explicitly foreign
department is refused. The response carries the real selected department and UTC
generation time. No request accepts an actor or role override.

A single SQL UNION ALL and metadata projection reads positive active shelf stock
and issued, unconsumed reservations. The existing `UnissuedReservations` rules
exclude holds still physically on source shelves, preventing double counting and
premature attribution to production. This same distinction applies to expiry
ownership labels and assigned-department access. Returned rows combine only the
same batch, department and unit ID. The response includes manufacturer, supplier,
manufacture, expiry, retest, AR number and GRN number. A migrated batch's corrected
AR takes priority, including a recorded clear. Otherwise the report uses active
sampling or the reference from the approved import. GRN is the receipt number for
ordinary stock and the saved reference for migrated stock. Existing effective
validity rules determine the expired subset.

The companion desktop presents these as **Item Code | Material | GRN No | Batch |
AR Number | MFG | EXP | Manufacturer | Supplier | Quantity**. Item Code is the
material code.

## Migrated batch corrections

Use **Correct migrated batch details** on the batch expiry detail to change the
manufacturing month, expiry month or GRN reference. Supply a reason. Manufacture
uses the first of the selected month and expiry uses the last. The endpoint
`PUT /api/v1/material/batch/{batchId}/migration-details` requires
`CanUpdateMigrationBatchDetails` and validates the actor's warehouse scope,
prior saved values, date order and retest restrictions. The correction and its
audit entry save together. The approved import remains available for review.
`AddMigrationBatchDetailsCorrections` grants the new permission to roles with
the existing migration retest correction permission. Apply it before the
desktop release. The focused PostgreSQL run for this release passed 56 tests
covering this workflow, report data and product batch stems.

Focused verification: department/kind/validity/ownership, fresh account changes,
unknown/disabled account refusal, reservation issue/release lifecycle, unit
separation, source/AR metadata, unissued hold regression, controller permission
policies, and Npgsql SQL translation. Broader material repository tests have 12
pre-existing failures, reproduced by identical test names on unchanged parent
`cc99b0119177aeed896cf576b470e29aab85db98`. They are not reported as passing.
