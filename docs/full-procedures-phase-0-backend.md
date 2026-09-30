# Full Procedures backend Phase-0 proof

Date: 2026-09-15. Runtime status: isolated .NET semantics proof; **not a production runtime**.

2026-09-16 template-area update: the reviewed Full Procedures capability keys
remain fail-closed, with separate area-management and template-review keys. The
Template Area configuration foundation is now persisted and API-wired through
migration `20260916064646_AddFullProcedureTemplateAreas`; see
`full-procedures-template-areas.md`. This supersedes the earlier unwired area
policy note, but adds no Procedure execution authority.

2026-09-16 question-revision update: the first real immutable Template Question
storage/API slice is implemented through migration
`20260916072607_AddFullProcedureQuestionRevisions`. It adds no Procedure runtime
authority and does not replace legacy Questions. See
`full-procedures-question-revisions.md` for the lifecycle, permissions, exact
calculation-revision pins, audit contract and verification.

2026-09-16 Section/Form update: immutable Template Section and Form revisions
are implemented through migrations
`20260916074547_AddFullProcedureSectionRevisions` and
`20260916123348_AddFullProcedureFormRevisions`. Sections pin exact Published
Question revisions; Forms pin exact Published Section revisions and exact
source Question revisions for earlier-section conditions. Both use hash-fenced
lifecycle commands, separate author/reviewer/publisher authority, dependency
revalidation at publish and append-only audit snapshots. See
`full-procedures-section-revisions.md` and
`full-procedures-form-revisions.md`. This remains definition governance only.

2026-09-16 Activity update: immutable Activity revisions now pin exact
Published Forms and govern typed action occurrences, performer/checker/approver
roles, resource capabilities, typed data contracts and completion rules. The
additive migration is isolated from the separately generated
`20260916132115_AddAnalysisTypeToFormSection` migration. EF reports no pending
model changes; the local database was unavailable for an apply check. See
`full-procedures-activity-revisions.md` for this isolation boundary.

2026-09-16 Workflow update: the next layer, immutable Workflow revisions, is
implemented through migration `20260916141007_AddFullProcedureWorkflowRevisions`.
A revision is a typed node/edge graph (Start, End, Activity, Branch, Fork/Join,
Wait/IPC, Hold/Resume, Rework) where Activity nodes pin an exact Published
Activity revision; node layout is persisted separately and excluded from the
content hash. The migration also adds the
`AK_TemplateActivityRevisions_Id_TemplateActivityId` alternate key the Activity
layer was missing for this exact-pin pattern; `dotnet ef migrations
has-pending-model-changes` reports none outstanding. See
`full-procedures-workflow-revisions.md` for the graph validation rules,
lifecycle and recorded evidence limits.

2026-09-16 sharing update: governed cross-area definition sharing is implemented
through migration `20260916174624_AddFullProcedureTemplateSharing`. Grants pin
one exact Published revision and hash, require a different actor in the other
area to decide, use optimistic versions and reasoned hashed audit snapshots,
and can be revoked by an area Publisher. Usage impact is aggregate definition
metadata only and cannot expose completed responses. The migration is
model-clean but was not applied in this slice. Six focused tests pass; the
aggregate suite at that milestone was **78 passed, 10 skipped, 0 failed**. See
`full-procedures-template-sharing.md`.

2026-09-16 adoption update: target-owned Draft adoption is implemented through
migration `20260916234752_AddFullProcedureTemplateAdoption`. One current Active
grant can create one target-owned Question, Section, Form, Activity or Workflow
Draft with immutable source lineage and exact dependency/role translations.
The serializable operation rechecks the Published source and atomically creates
the Draft, advances/audits the grant and records lineage. It copies no source
approvals, actor IDs, responses, evidence or consumers. The migration is
model-clean and was not applied in this slice. Seven focused tests pass. See
`full-procedures-template-adoption.md`.

2026-09-16 Procedure-definition update: immutable Procedure revisions now pin
one exact Published Workflow and hash, declare product/site applicability by
Batch Type, and assign every Activity node to one manufacturing, packaging,
shared or development record scope. Separate permissions govern create, edit,
validate, review, approve and retire; approval rechecks dependencies and
transactionally retires a superseded Approved revision. Migration
`20260916234859_AddFullProcedureDefinitions` is model-clean and was not applied
in this slice. Six focused tests pass, bringing the aggregate suite to **91
passed, 10 skipped, 0 failed**. This definition layer does not create BMR/BPR
masters or issue/execute a batch. See `full-procedure-definitions.md`.

`APP/Services/FullProcedures/ProcedureRuntimeSpike.cs` introduces no controller, DI registration, EF model, migration, event publisher or material/quality adapter. `ProcedureSqlSpikeStore.cs` and `ProcedureSqlOutboxSpike.cs` are separate Npgsql candidates hard-gated to a database named `oryx_procedure_spike_test`; neither is registered or backed by a production migration. Legacy Routes remain unchanged and continue to own existing batches. The new code cannot issue a batch. The frontend's `docs/full-procedures/runtime-decision.md` records the conditional runtime recommendation and contract-freeze gates.

The semantics proof models exact Procedure revision/content pinning, unique stage occurrence IDs, predecessor and parallel-join gates, independent QC-result command shape, due IPC occurrence, abort with remarks, optimistic expected-run version, command-ID retry and JSON state rehydration. Its six targeted xUnit tests pass. The SQL proof uses a **disposable local PostgreSQL cluster**, not the ERP database. Its test-only schema stores run state, audit and outbox in one transaction; a forced fault before outbox rolls all three back. A fresh data source reloads committed state, an identical retry writes no second event, competing writers yield one version/event, and an effect key cannot acquire two different receipt IDs. The outbox candidate leases a row with `FOR UPDATE SKIP LOCKED`, calls a fake idempotent effect adapter with a stable key, records a local receipt, and fences acknowledgement by attempt. A separate `tests/ProcedureSpikeWorker` executable is now killed at **three** windows: after claim/before fake effect, after fake effect/before local receipt, and after receipt/before acknowledgement. A new process reloads the run and claims the same event after lease expiry. Each test observes two delivery attempts but one fake effect. The fake effect is a separate table/connection in the same disposable database, **not** an external warehouse or QC service.

`ProcedureActionContractSpike.cs` adds a separate, pure issue/interpretation proof. One stage may have multiple unique action occurrences. Issue requires exact action ID/version/kind/content hash and an available runtime/action interpreter, retains the action JSON in a detached manifest, and refuses malformed/duplicate JSON properties. Resolve uses the issued snapshot—not a newer catalog—and returns explicit blocked codes for manifest drift, wrong run, missing action or unsupported interpreter version. Six new targeted tests pass both in an isolated linked-source project and in one successful shared no-database run; this is not a governed catalog, approved BMR/BPR bundle, API guard or authorized action execution.

Targeted verification: `dotnet test tests/APP.Tests/APP.Tests.csproj --filter FullyQualifiedName~FullProcedures --no-restore`. On 2026-09-16, after Workflow hardening, governed sharing, target-owned adoption and immutable Procedure definitions, the real in-tree project built and reported **91 passed, 10 skipped, 0 failed**. The ten skipped tests are explicitly gated disposable-PostgreSQL/process/server-restart cases because no spike database URL/data directory was supplied. The passing tests include the permission catalog, forbidden-result mapping, server area policy, persistent Template Area, Question, Section, Form, Activity, Workflow, sharing, adoption and Procedure-definition services, semantic-hash/layout regressions, runtime semantics and action-contract proofs. Tests did not execute live Procedure effects, apply the new migrations or prove a real PostgreSQL adoption/approval race.

An earlier isolated source-linked harness ran the then-current 21 tests against disposable PostgreSQL and passed all 21. The shared build issue that required that harness has since cleared; the current in-tree result is recorded above. The ten database-dependent tests have not been rerun against PostgreSQL after adding the eight permission/area tests, which themselves do not use PostgreSQL.

## Database-server-restart proof (added 2026-09-16)

The product/process owner chose to keep hardening this .NET coordinator rather than benchmark Temporal now, so the next ADR-001 exit test was tackled directly: `ProcedureSqlServerRestartTests.cs` proves the disposable PostgreSQL **server process** itself, not just a killed .NET worker, can crash and restart safely. It runs `pg_ctl -m immediate stop` (skips a checkpoint — a real crash, not a clean shutdown) then `pg_ctl start` against the same disposable cluster, and asserts: (1) previously committed state, its audit row and its outbox row all survive intact, and retrying the same command afterward stays idempotent (no second row, same version); (2) a transaction crashed mid-flight — inside the existing `beforeOutbox` fault hook, before the outbox insert — leaves zero trace (no run-version bump, no audit row, no outbox row) after restart, and a subsequent retry then commits exactly once.

Two real bugs surfaced building this, both fixed in the test harness itself (not in `ProcedureSqlSpikeStore.cs`, which needed no change): `pg_ctl start` does **not** automatically reapply the listen address/port recorded in the data directory's `postmaster.opts` — it silently tried to bind the default port 5432 and failed, so the test now always passes `-o "-p <port> -h <host>"` explicitly, derived from the same `ORYX_PROCEDURE_SPIKE_TEST_DATABASE_URL`; and redirecting the child `pg_ctl`/`postgres` process's stdout/stderr without draining them deadlocked the restart, since postgres logs verbosely during crash recovery — fixed by reading both streams concurrently with the exit wait, and by always giving `pg_ctl start` an explicit `-l <logfile>`. A new `DisposablePostgresServerControlFactAttribute` gates these tests behind an additional `ORYX_PROCEDURE_SPIKE_TEST_PGDATA_DIR` variable (the disposable cluster's own data directory) so they never run, even accidentally, against a shared or production PostgreSQL instance.

Verified together with the then-existing 21 in the same isolated, source-linked harness described above: **23 passed, 0 failed**. The disposable cluster, its data directory and the temporary harness were all removed afterward.

This closes ADR-001 boundary #3's basic crash-safety claim (committed state and in-flight rollback survive a real server crash), but not its full scope: holds/resume, recurring IPC, controlled rework, parallel joins, QA/QC waits, abort closeout and long-duration timers have not been tested under a server restart, only the same single-work-node scenario the earlier killed-worker tests used. That remains open.

## Evidence limits and next backend gate

- `ApprovedQcReceiptId` is **only a reference requirement**. The proof cannot verify QC approval, sample/specification context, company/site access or independent reviewer. A production adapter must resolve these server-side before accepting the wait.
- Definition SHA-256 is a prototype drift detector, not a governed approval signature. A real release must bind immutable Procedure, BMR/BPR, activity, form and policy content in an approved manifest.
- PostgreSQL row locking/version comparison and state/audit/outbox rollback have been proved in the disposable schema. Killing a separate worker proves recovery at three fake-effect delivery points; a real server crash/restart now additionally proves the base commit/rollback/retry case (see above), but not database restart under holds/timers/parallel joins, production schema/migration, replay scheduling or real outbox delivery.
- A leased retry with a fake idempotent adapter and local effect-receipt uniqueness does not prove that an external warehouse/quality adapter is idempotent or that physical work is deduplicated. No real effect, ledger reconciliation or dispatch was performed.
- Abort stops ordinary stage work in the proof; it does not generate material/sample closeout or authorize disposition.
- IPC is one due occurrence. Recurrence, holds, rework and durable timers remain open runtime requirements. The action-contract snapshot test does not prove governed catalog publication, BMR/BPR issue binding, handler compatibility across deployments or actor authorization.

The ADR gives this persisted .NET coordinator a **conditional first-candidate** position against Temporal/Dapr, based on current infrastructure and these fixtures. Require database restart/timer/wait recovery, real outbox delivery/replay, governed action-contract/version retention, and idempotent domain-adapter receipts before freezing Procedure run/event/effect contracts. The disposable cluster was shut down and removed after testing; its test data is not retained. No pilot should issue from this proof.

Documentation updated for the Question/Section/Form slices: this file, backend `README.md`,
`docs/services.md`, `docs/workflows.md`,
the three definition-revision notes, and the corresponding frontend Full
Procedures planning/status documents. The definition migrations were applied
only to the configured local development database after SQL review; no
production deployment was performed.
