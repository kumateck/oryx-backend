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
same batch, department and unit ID. Manufacturer/supplier, deterministic active AR
number and original manufacture/expiry/retest dates accompany each quantity.
Existing effective-validity rules determine the expired subset. No stock/audit
writes or schema migration are introduced; old reports retain their APIs.

Focused verification: department/kind/validity/ownership, fresh account changes,
unknown/disabled account refusal, reservation issue/release lifecycle, unit
separation, source/AR metadata, unissued hold regression, controller permission
policies, and Npgsql SQL translation. Broader material repository tests have 12
pre-existing failures, reproduced by identical test names on unchanged parent
`cc99b0119177aeed896cf576b470e29aab85db98`. They are not reported as passing.
