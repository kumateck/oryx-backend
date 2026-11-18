# Repair Unstart leftovers

The warehouse issue page offers a reviewed correction for stock requests left active
by an earlier recorded production Unstart. This is a separate action from Unstart;
installing it does not change any customer records automatically.

The preview inspects up to 200 requests in the signed-in user's current department.
Raw and packaging are determined from stored material kinds, never request codes.
A request must predate a recorded Unstart for its batch and the recorded run must be
archived. Newer requests, trial requests and White Placebo requests are excluded.
Restarted batches, mixed/missing materials, active reservations, any issue record
(including archived issues), received quantities and issued bin-card evidence block
removal. Blocked requests are shown but cannot be selected.

Apply accepts selected request IDs, a current preview hash, an idempotent request ID
and a required 3–1000 character reason. A serializable transaction locks production
runs, Unstart receipts, requests/lines, reservations, issues and bin cards, rebuilds
the preview and rechecks current grants before writing. Any stale or blocked choice
refuses the entire request. Eligible requests and active lines are soft-deleted with
the actor and timestamp; logs are appended to the original run. No shelf quantity,
reservation, movement or bin-card balance is changed. The immutable repair receipt
retains actor, department, variant, reason, selected IDs and the reviewed snapshot.
Exact retries return the original receipt; conflicting request ID reuse fails.

## API and access

- `GET /api/v1/unstart-leftovers/raw`: `CanViewRawMaterialRequisitions`.
- `GET /api/v1/unstart-leftovers/packaging`: `CanViewPackagingMaterialRequisitions`.
- `POST /api/v1/unstart-leftovers/raw`: `CanRepairUnstartRawMaterialRequisitions`.
- `POST /api/v1/unstart-leftovers/packaging`: `CanRepairUnstartPackagingMaterialRequisitions`.

The new repair permissions are registered under **Warehouse / Issue Stock
Requisitions** and granted to nobody. Apply also requires the matching existing view
permission. No administrator bypass or role-name inference is used. Existing issue
and view permissions are unchanged. The desktop uses the same dedicated action
permission and rechecks it immediately before sending a repair.

Apply migration `20261118140306_AddUnstartLeftoverRepairs` before the companion UI.
It adds the receipt table and database UPDATE/DELETE/TRUNCATE guards; it does not
cancel existing requests or grant permissions. Requests without recorded Unstart
evidence need separate investigation and are never guessed from missing runs.

The UI invalidates issue, requisition, pending-approval and swap caches after success.
The metadata-only OpenAPI importer is `scripts/import-unstart-leftovers.ts` in the
desktop repository; run it against the matching export, then regenerate the clients.
