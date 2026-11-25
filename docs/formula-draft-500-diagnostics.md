# Capture formula draft 500 logs on a customer server

This read-only helper captures the backend API container's logs for one
reproduction of a formula draft-save failure. It does not read or print
environment variables, credentials, formula contents or database records.

From the backend release directory, run:

```sh
./deploy/diagnose-formula-draft-500.sh
```

When prompted, reproduce the draft save once, wait for the 500, then press
Enter in the terminal. The script writes a timestamped log under
`~/.local/state/oryx-backend/diagnostics/`, with directory mode `0700` and
file mode `0600`. If the current user cannot access Docker, use the server's
approved sudo procedure; running it as root stores the capture under root's
state directory. Set `ORYX_BACKEND_API_CONTAINER` if the API container has a
different name.

The helper prints matching exception and trace lines but retains the restricted
full capture. Compare the trace ID with the failed HTTP response if available.
If no relevant lines appear, preserve the capture and check the server's
configured container logging destination with its administrator. Treat the
capture as customer data and share it only through the approved private support
channel.

## Status-audit rejection during an ordinary draft edit (2026-11-25)

The customer capture shows `PUT /api/v1/formula-questions/{id}` failing in
`FormulaDefinitionService.UpdateDraftAsync` with PostgreSQL SQLSTATE `55000`:
`Formula revision status transition requires a matching audit row in the same transaction`.
The logged batch inserts a `DraftUpdated` audit and updates the definition,
authoring payload and timestamp; it does not update `Status`. The error occurs
during that batch, before the controller commits its transaction.

The original migration defines `TR_FormulaRevisions_StatusAudit` as a deferred
constraint trigger with `WHEN (OLD."Status" IS DISTINCT FROM NEW."Status")`.
An unchanged-status update must not call its transition check. The failure is
therefore inconsistent with that trigger definition; the live catalog is needed
to identify the exact difference from a clean installation. On the database
selected from the installed API's connection, an administrator can inspect it
without reading business records:

```sql
SELECT current_database(), t.tgname, t.tgenabled, pg_get_triggerdef(t.oid)
FROM pg_trigger t
WHERE t.tgrelid = 'public."FormulaRevisions"'::regclass AND NOT t.tgisinternal;
SELECT pg_get_functiondef('public.oryx_formula_v1_require_revision_audit()'::regprocedure);
```

`20261125110000_RepairFormulaRevisionStatusAuditTrigger` is a forward migration
for already-installed databases: it recreates the named trigger
with its original status-only, deferred semantics and makes the function itself
return immediately for an unchanged status. It preserves every matching-audit
condition for actual transitions, including the current transaction requirement.
It changes no formula contents, revision statuses, audit rows, permissions,
controller contract or model snapshot. Previously applied migrations are untouched.

Deliver it through the normal backend migration/deployment process described in
[customer backend deployment](customer-backend-deployment.md). Updating frontend
assets or restarting the current API does not install this database repair.
Do not disable the integrity trigger or synthesize audit evidence to work around
the failure. The customer database has not been modified by preparing this fix.

PostgreSQL regression coverage reproduces an immediate, unfiltered audit trigger,
then verifies the repaired endpoint saves a draft and its `DraftUpdated` audit.
It also checks repeat application, unchanged-status defense inside the function,
deferred enforcement, rejection of missing/stale/mismatched audit evidence, and
the existing review, approval and supersession lifecycle. The existing
`CanEditQuestions` endpoint and submit permissions remain unchanged.
